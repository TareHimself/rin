#include "Framework/Shaders/operators.slang"
#include "Framework/Shaders/utils.slang"

struct Cubic {
    float2 p0;
    float2 p1;
}

struct Segment {

}

struct PushConstants {
    SkinnedMesh *skinnedMeshes;
    int totalInvocations;
}

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct ComputeIn {
    uint3 threadId : SV_DispatchThreadID;
}

void fitCubicToEulerSpiral(float2 lastP, float2 nextP, float2 lastQ, float2 nextQ, float dt,out float2 th0,out float2 th1, out float coordLen, out float error) {


}
void evalCubicAndDeriv(Cubic cubic, float offset, out float2 nextP,out float2 nextQ) {
    
}

void flattenCubic(Cubic cubic, float offset) {

}

[shader("compute")]
[numthreads(256, 1, 1)]
void compute(ComputeIn input)
{
    
}