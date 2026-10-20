namespace GroupSharedCheck::GroupSharedShader
{
    struct ReducePush
    {
        float* input;
        float* output;
    }

    struct ComputeIn
    {
        uint threadId : SV_DispatchThreadID;
        int groupIndex : SV_GroupIndex;
        uint groupId : SV_GroupID;
    }

}

using namespace GroupSharedCheck::GroupSharedShader;
using namespace GroupSharedCheck;

groupshared float partial[64];
groupshared uint count;

[[vk::push_constant]] uniform ConstantBuffer<ReducePush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(64, 1, 1)]
void compute(ComputeIn input)
{
    partial[input.groupIndex] = push.input[input.threadId];
    if (input.groupIndex == 0)
    {
        count = 0;
    }
    GroupMemoryBarrierWithGroupSync();
    InterlockedAdd(count, 1);
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
    push.output[input.groupId] = sum;
}
