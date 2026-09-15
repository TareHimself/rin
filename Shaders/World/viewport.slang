#include "Shaders/Core/Attributes/blending.slang"
#include "Shaders/Core/images.slang"
#include "Shaders/Core/operators.slang"
#include "Shaders/Core/utils.slang"
#include "Shaders/World/Lights/Models/disney.slang"
#include "Shaders/World/Lights/Models/lambert.slang"
#include "Shaders/World/gbuffer_in.slang"

struct PushData {
    float4x4 projection;
    float4x4 transform;
    float2 size;
    ImageHandle outputImage;
    ImageHandle gBuffer0;
    ImageHandle gBuffer1;
    ImageHandle gBuffer2;
    ImageHandle gBuffer3;
    PackedLight *lights;
    int lightCount;
    int channel;
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

// Mirrors Rin.World.Views.ViewportChannel
#define VIEWPORT_CHANNEL_SCENE 0
#define VIEWPORT_CHANNEL_COLOR 1
#define VIEWPORT_CHANNEL_LOCATION 2
#define VIEWPORT_CHANNEL_NORMAL 3
#define VIEWPORT_CHANNEL_RMS 4
#define VIEWPORT_CHANNEL_EMISSIVE 5
#define VIEWPORT_CHANNEL_RADIANCE 6

[shader("fragment"), Attachment(AttachmentFormat.RGBA32),BlendUI,Stencil]
float4 fragment(in float2 iUV: UV,float2 coordinate: SV_Position)
    : SV_Target
{
    switch (push.data->channel) {
    case VIEWPORT_CHANNEL_COLOR:
        return float4(push.data->gBuffer0.SampleTexture(iUV).rgb, 1.0);
    case VIEWPORT_CHANNEL_LOCATION:
        return float4(push.data->gBuffer1.SampleTexture(iUV).rgb, 1.0);
    case VIEWPORT_CHANNEL_NORMAL:
        return float4(push.data->gBuffer2.SampleTexture(iUV).rgb, 1.0);
    case VIEWPORT_CHANNEL_RMS:
        return float4(push.data->gBuffer0.SampleTexture(iUV).a, push.data->gBuffer1.SampleTexture(iUV).a, push.data->gBuffer2.SampleTexture(iUV).a, 1.0);
    case VIEWPORT_CHANNEL_EMISSIVE:
        return float4(push.data->gBuffer3.SampleTexture(iUV).rrr, 1.0);
    case VIEWPORT_CHANNEL_RADIANCE: {
        var location = push.data->gBuffer1.SampleTexture(iUV).rgb;
        var normal = push.data->gBuffer2.SampleTexture(iUV).rgb;
        var radiance = float3(0.0);
        var lights = push.data->lights;
        for (var i = 0; i < push.data->lightCount; i++) {
            var light = lights[i];
            var L = -getLightDirectionToLocation(light, location);
            var NoL = max(dot(normal, L), 0.0);
            radiance += NoL * light.getRadiance() * light.getColor() * getLightAttenuation(light, location);
        }
        return float4(radiance, 1.0);
    }
    default:
        return push.data->outputImage.SampleTexture(iUV);
    }
}
