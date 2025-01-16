import rin.utils;
import rin.operators;
import gbuffer_in;
import rin.scene.light;
import rin.scene.models.lambert;
import rin.scene.models.pbr;


public enum ViewportChannel : int
{
    Scene,
    Color,
    Location,
    Normal,
    RoughnessMetallicSpecular,
    Emissive
}

struct RenderInfo
{
    // [TextureId,RenderMode,0,0]
    ViewportChannel channel;
    float2 size;
    float3x3 transform;
    float4x4 view;
    float4x4 projection;
    int numLights;
};

struct PushConstants
{
    float4x4 projection;
    RenderInfo* quads;
    Light* lights;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int instanceId: SV_InstanceID, int vertexId: SV_VertexID, out float2 oUV: UV, out int oQuadIndex: QUAD_INDEX)
    : SV_Position
{
    RenderInfo quad = push.quads[instanceId];
    float4 position;
    generateRectVertex(quad.size, push.projection, quad.transform, vertexId, position, oUV);
    oQuadIndex = instanceId;
    return position;
}

[shader("fragment")]
float4 fragment(in float2 iUV: UV, in int iQuadIndex: QUAD_INDEX, float2 coordinate: SV_Position)
    : SV_Target
{
    RenderInfo quad = push.quads[iQuadIndex];
    var RGB_COLOR_A_ROUGHNESS_SAMPLE = RGB_COLOR_A_ROUGHNESS.Sample(iUV);
    var RGB_LOCATION_A_METALLIC_SAMPLE = RGB_LOCATION_A_METALLIC.Sample(iUV);
    var RGB_NORMAL_A_SPECULAR_SAMPLE = RGB_NORMAL_A_SPECULAR.Sample(iUV);
    switch (quad.channel)
    {
    case ViewportChannel.Scene:
    {
        // LambertModel model;
        
        // LambertSurface surface;

        // float3 color = float3(0.0f);

        // var eye = (quad.view * float4(float3(0.0f),1.0f)).xyz;
        // var location = RGB_LOCATION_A_METALLIC_SAMPLE.xyz;

        // surface.location = location;
        // surface.normal = normalize(RGB_NORMAL_A_SPECULAR_SAMPLE.xyz);
        // surface.color = RGB_COLOR_A_ROUGHNESS_SAMPLE.xyz;

        // for (int i = 0; i < quad.numLights; i++) {
        //     color += model.eval(surface, eye, push.lights[i]);
        // }

        // color /= quad.numLights;

        // return float4(color,1.0);

        var eye = (quad.view * float4(float3(0.0f), 1.0f)).xyz;
        var location = RGB_LOCATION_A_METALLIC_SAMPLE.xyz;

        

        PBRModel model;

        PBRSurface surface;

        surface.location = location;
        surface.normal = normalize(RGB_NORMAL_A_SPECULAR_SAMPLE.xyz);
        surface.color = RGB_COLOR_A_ROUGHNESS_SAMPLE.xyz;
        surface.roughness = RGB_COLOR_A_ROUGHNESS_SAMPLE.w;
        surface.metallic = RGB_LOCATION_A_METALLIC_SAMPLE.w;
        surface.specular = RGB_NORMAL_A_SPECULAR_SAMPLE.w;
        surface.emissive = 0.0f;

        float3 color = float3(0.0f);

        for (int i = 0; i < quad.numLights; i++) {
            color += model.eval(surface, eye, push.lights[i]);
        }

        color /= quad.numLights;

        return float4(color, 1.0);
    }
    case ViewportChannel.Color:
    {
        return float4(RGB_COLOR_A_ROUGHNESS_SAMPLE.xyz, 1.0);
    }
    case ViewportChannel.Location:
    {
        return float4(RGB_LOCATION_A_METALLIC_SAMPLE.xyz, 1.0);
    }
    case ViewportChannel.Normal:
    {
        return float4(RGB_NORMAL_A_SPECULAR_SAMPLE.xyz, 1.0);
    }
    case ViewportChannel.RoughnessMetallicSpecular:
    {
        return float4(RGB_COLOR_A_ROUGHNESS_SAMPLE.w, RGB_LOCATION_A_METALLIC_SAMPLE.w, RGB_NORMAL_A_SPECULAR_SAMPLE.w, 1.0);
    }
    case ViewportChannel.Emissive:
    {
        return float4(0.0);
    }
    default:
        return float4(1.0);
    }
}
