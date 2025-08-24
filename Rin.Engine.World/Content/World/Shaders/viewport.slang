#include "Framework/Shaders/Attributes/blending.slang"
#include "Framework/Shaders/images.slang"
#include "Framework/Shaders/operators.slang"
#include "Framework/Shaders/utils.slang"
#include "World/Shaders/Lights/Models/disney.slang"
#include "World/Shaders/Lights/Models/lambert.slang"
#include "World/Shaders/gbuffer_in.slang"

struct PushData {
    float4x4 projection;
    float4x4 transform;
    float2 size;
    ImageHandle outputImage;
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

[shader("fragment"), Attachment(AttachmentFormat.RGBA32),BlendUI,Stencil]
float4 fragment(in float2 iUV: UV,float2 coordinate: SV_Position)
    : SV_Target
{
    var data = push.data.outputImage.SampleTexture(iUV);
    return data;
}
