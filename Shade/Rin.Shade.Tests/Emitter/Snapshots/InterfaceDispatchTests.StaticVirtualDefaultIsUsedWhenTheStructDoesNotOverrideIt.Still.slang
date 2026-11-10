[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
            struct TestBuffer_Still
            {
                uint handle;
                uint offset;
            }

            struct TestBuffer_float3
            {
                uint handle;
                uint offset;
            }

            struct TestHeap
            {
                RWByteAddressBuffer buffers[8];
            }

        }

    }

}

namespace InterfaceDispatchCheck
{
    struct OffsetPush_Still
    {
        Rin::Shade::Tests::TestBuffer_Still materials;
        Rin::Shade::Tests::TestBuffer_float3 output;
    }

    struct Still
    {
        float unused;
    }

}

using namespace InterfaceDispatchCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

float3 IVertexOffset_Still_offset(in Still material, float3 world)
{
    return float3(0);
}

extension Rin::Shade::Tests::TestBuffer_float3
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)12;
    }

    void setItem(uint index, float3 value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<float3>(byteOffset(index), value);
    }

    void setItem(int index, float3 value)
    {
        setItem((uint)index, value);
    }
}

extension Rin::Shade::Tests::TestBuffer_Still
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)4;
    }

    Still getItem(uint index)
    {
        return rin_testHeap.buffers[(int)this.handle].Load<Still>(byteOffset(index));
    }

    Still getItem(int index)
    {
        return getItem((uint)index);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<OffsetPush_Still, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var world = float3(1, 2, 3);
    push.output.setItem(0, world + IVertexOffset_Still_offset(push.materials.getItem(0), world));
}
