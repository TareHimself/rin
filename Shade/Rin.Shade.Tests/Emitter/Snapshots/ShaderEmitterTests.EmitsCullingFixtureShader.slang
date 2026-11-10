[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
            struct TestBuffer_Bounds3D
            {
                uint handle;
                uint offset;
            }

            struct TestBuffer_uint
            {
                uint handle;
                uint offset;
            }

            struct TestHeap
            {
                RWByteAddressBuffer buffers[8];
            }

            namespace Fixtures
            {
                struct CullingPushConstants
                {
                    TestBuffer_Bounds3D bounds;
                    uint invocationCount;
                    TestBuffer_uint output;
                }

                struct ComputeIn
                {
                    uint threadId : SV_DispatchThreadID;
                }

                struct Bounds3D
                {
                    float3 min;
                    float3 max;
                }

            }

        }

    }

}

using namespace Rin::Shade::Tests::Fixtures;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

extension Rin::Shade::Tests::TestBuffer_Bounds3D
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)24;
    }

    Bounds3D getItem(uint index)
    {
        return rin_testHeap.buffers[(int)this.handle].Load<Bounds3D>(byteOffset(index));
    }

    Bounds3D getItem(int index)
    {
        return getItem((uint)index);
    }
}

extension Rin::Shade::Tests::TestBuffer_uint
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)4;
    }

    void setItem(uint index, uint value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<uint>(byteOffset(index), value);
    }

    void setItem(int index, uint value)
    {
        setItem((uint)index, value);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<CullingPushConstants, ScalarDataLayout> push;

[shader("compute")]
[numthreads(64, 1, 1)]
void compute(ComputeIn input)
{
    if (input.threadId >= push.invocationCount)
    {
        return;
    }
    var bounds = push.bounds.getItem((int)input.threadId);
    push.output.setItem((int)input.threadId, 1);
}
