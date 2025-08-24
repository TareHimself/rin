#include "Framework/Shaders/utils.slang"
#include "Framework/Shaders/operators.slang"
[[vk::binding(0, 0)]]
uniform Sampler2D SceneImage;

struct PushConstants
{
    float4x4 projection;
    float2 size;
    float3x3 transform;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{
    float4 position;
    generateRectVertex(push.size, push.projection, push.transform, vertexId, position, oUV);
    return position;
}

[shader("fragment")]
float4 fragment(in float2 iUV: UV,float2 coordinate: SV_Position)
    : SV_Target
{
    return float4(SceneImage.Sample(iUV).xyz,1.0f);
}
