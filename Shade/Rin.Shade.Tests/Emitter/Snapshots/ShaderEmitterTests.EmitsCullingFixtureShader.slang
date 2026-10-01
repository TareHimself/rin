namespace Rin::Shade::Tests::Fixtures
{
    struct Bounds3D
    {
        float3 min;
        float3 max;
    }

    struct CullingPushConstants
    {
        Bounds3D* bounds;
        uint invocationCount;
        uint* output;
    }

    struct ComputeIn
    {
        uint threadId : SV_DispatchThreadID;
    }

}

using namespace Rin::Shade::Tests::Fixtures;

[[vk::push_constant]] uniform ConstantBuffer<CullingPushConstants, ScalarDataLayout> push;

[shader("compute")]
[numthreads(64, 1, 1)]
void compute(ComputeIn input)
{
    if (input.threadId >= push.invocationCount)
    {
        return;
    }
    var bounds = push.bounds[(int)input.threadId];
    push.output[(int)input.threadId] = 1;
}
