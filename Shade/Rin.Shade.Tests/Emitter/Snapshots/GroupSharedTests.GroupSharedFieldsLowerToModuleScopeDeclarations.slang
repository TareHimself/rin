[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
            struct TestBuffer_float
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

namespace GroupSharedCheck
{
    namespace GroupSharedShader
    {
        struct ReducePush
        {
            Rin::Shade::Tests::TestBuffer_float input;
            Rin::Shade::Tests::TestBuffer_float output;
        }

        struct ComputeIn
        {
            uint threadId : SV_DispatchThreadID;
            int groupIndex : SV_GroupIndex;
            uint groupId : SV_GroupID;
        }

    }

}

using namespace GroupSharedCheck::GroupSharedShader;
using namespace GroupSharedCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

groupshared float partial[64];
groupshared uint count;

extension Rin::Shade::Tests::TestBuffer_float
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)4;
    }

    float getItem(uint index)
    {
        return rin_testHeap.buffers[(int)this.handle].Load<float>(byteOffset(index));
    }

    void setItem(uint index, float value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<float>(byteOffset(index), value);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<ReducePush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(64, 1, 1)]
void compute(ComputeIn input)
{
    partial[input.groupIndex] = push.input.getItem(input.threadId);
    if (input.groupIndex == 0)
    {
        count = 0u;
    }
    GroupMemoryBarrierWithGroupSync();
    InterlockedAdd(count, 1u);
    GroupMemoryBarrierWithGroupSync();
    if (input.groupIndex != 0)
    {
        return;
    }
    float sum = 0;
    for (var i = 0; i < (int)count; i++)
    {
        sum += partial[i];
    }
    push.output.setItem(input.groupId, sum);
}
