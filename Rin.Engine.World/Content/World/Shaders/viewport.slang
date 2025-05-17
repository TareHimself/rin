#include "World/Shaders/gbuffer_in.slang"
#include "Engine/Shaders/operators.slang"
#include "Engine/Shaders/images.slang"
#include "Engine/Shaders/utils.slang"

struct PushData {
    float4x4 projection;
    float4x4 transform;
    float2 size;
    GBuffer gBuffer;
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
    var sample = push.data->gBuffer.sample(iUV);

    //return float4(push.data->gBuffer.G_BUFFER_0.SampleTexture(iUV).rgb,1.0);
    return float4(sample.color,1.0f);
}
