#include "Engine/Shaders/Attributes/blending.slang"
#include "Engine/Shaders/images.slang"
#include "Engine/Shaders/operators.slang"
#include "Engine/Shaders/utils.slang"
#include "World/Shaders/Lights/Models/disney.slang"
#include "World/Shaders/Lights/Models/lambert.slang"
#include "World/Shaders/gbuffer_in.slang"

struct PushData {
    GBuffer gBuffer;
    float3 eye;
    PackedLight *lights;
    int lightCount;
}

struct PushConstants
{
    PushData *data;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;
const static float2 _lighting_internal_vertices[] = { float2(-1.0), float2(1.0, -1.0), float2(1.0), float2(-1.0), float2(1.0), float2(-1.0, 1.0) };
[shader("vertex")]
float4 vertex(int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{

    var coord = _lighting_internal_vertices[vertexId];
    var uv = (coord + 1.0) / 2.0;
    oUV = uv;
    return float4(coord, 0.0, 1.0);
}

struct ModelSurface : IDisneySurface, ILambertSurface {
    GBufferSample sample;

    float3 getColor() {
        return sample.color;
    }
    float3 getLocation() {
        return sample.location;
    }
    float3 getNormal() {
        return sample.normal;
    }
    float getRoughness() {
        return sample.roughness;
    }
    float getMetallic() {
        return sample.metallic;
    }
    float getSpecular() {
        return sample.specular;
    }
    float getEmissive() {
        return 0.0;
    }
}
[shader("fragment"), Attachment(AttachmentFormat.RGBA32),BlendUI,Stencil]
float4 fragment(in float2 iUV: UV,float2 coordinate: SV_Position)
    : SV_Target
{
    var sample = push.data->gBuffer.sample(iUV);
    var surface = ModelSurface(sample);
    var model = DisneyModel();
    
    var lights = push.data->lights;
    var numLights = push.data->lightCount;
    var eye = push.data->eye;

    var color = float3(0.0);
    
    for (var i = 0; i < numLights; i++) {
        color += model.eval(surface, eye, lights[i]);
    }

    //return float4(float2(iUV),0.0,1.0f);
    //color /= numLights;
    //return float4(push.data->gBuffer.G_BUFFER_0.SampleTexture(iUV).rgb,1.0);
    return float4(color,1.0f);
}
