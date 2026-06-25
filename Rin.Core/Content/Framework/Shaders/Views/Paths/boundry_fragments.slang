#include "Framework/Shaders/Views/Paths/common.slang"

struct PushConstants
{
    Path *paths;
    Segment *segments;
    int segmentCount;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct ComputeIn {
    uint3 threadId : SV_DispatchThreadID;
}

[shader("compute")]
[numthreads(1, 1, 1)]
void compute(ComputeIn input)
{
}