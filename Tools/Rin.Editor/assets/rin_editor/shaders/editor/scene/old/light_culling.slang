import rin.operators;
import rin.attributes;
import rin.scene.light;

struct ComputeIn {
    uint3 threadId : SV_DispatchThreadID;
}

struct PushConstants
{
    float4x4 projection;
    int numLights;
    PackedLight *lights;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("compute")]
[numthreads(32, 1, 1)]
void compute(ComputeIn input)
{

}