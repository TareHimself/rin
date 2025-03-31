#include "Engine/Shaders/utils.slang"
#include "Engine/Shaders/operators.slang"
[[vk::binding(0, 0)]]
uniform Sampler2D SceneImage;

struct PushData {
    float4x4 projection;
    float4x4 transform;
    float2 size;
}
struct PushConstants
{
    PushData *data;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{
    float4 position;
    generateRectVertex(push.data->size, push.data->projection, push.data->transform, vertexId, position, oUV);
    return position;
}

[shader("fragment")]
float4 fragment(in float2 iUV: UV,float2 coordinate: SV_Position)
    : SV_Target
{
    return float4(SceneImage.Sample(iUV).xyz,1.0f);
}
