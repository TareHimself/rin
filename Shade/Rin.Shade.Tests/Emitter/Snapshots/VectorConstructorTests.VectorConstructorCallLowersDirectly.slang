[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
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

namespace VectorConstructorCheck
{
    struct VectorConstructorPush
    {
        Rin::Shade::Tests::TestBuffer_float3 output;
    }

}

using namespace VectorConstructorCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

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

[[vk::push_constant]] uniform ConstantBuffer<VectorConstructorPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var v = float3(1, 2, 3);
    push.output.setItem(0, v);
}
