import rin.utils;
import rin.operators;
import gbuffer_in;
import rin.scene.light;
import rin.scene.models.lambert;
import rin.scene.models.pbr_new;


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
    RenderInfo *quads;
    PackedLight* lights;
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

struct Surface : ILambertSurface {
    float4 colorRoughness;
    float4 locationMetallic;
    float4 normalSpecular;
    float3 getColor() {
        return colorRoughness.xyz;
    }
    float3 getLocation() {
        return locationMetallic.xyz;
    }
    float3 getNormal() {
        return normalize(normalSpecular.xyz);
    }
}

extension Surface : IPBRSurface {
    float getRoughness() {
        return colorRoughness.w;
    }
    float getMetallic() {
        return locationMetallic.w;
    }
    float getSpecular() {
        return normalSpecular.w;
    }
    float getEmissive() {
        return 0.0f;
    }
}

[shader("fragment")]
float4 fragment(in float2 iUV: UV, in int iQuadIndex: QUAD_INDEX, float2 coordinate: SV_Position)
    : SV_Target
{
    RenderInfo quad = push.quads[iQuadIndex];
    Surface surface = Surface(
        RGB_COLOR_A_ROUGHNESS.Sample(iUV), 
        RGB_LOCATION_A_METALLIC.Sample(iUV), 
        RGB_NORMAL_A_SPECULAR.Sample(iUV)
        );
    switch (quad.channel)
    {
    case ViewportChannel.Scene:
    {
        PBRModel model;
        
        float3 color = float3(0.0f);

        var eye = (quad.view * float4(float3(0.0f),1.0f)).xyz;

        for (int i = 0; i < quad.numLights; i++) {
            color += model.eval(surface, eye,push.lights[i]);
        }

        color /= quad.numLights;

        return float4(color,1.0);
    }
    case ViewportChannel.Color:
    {
        return float4(surface.getColor(), 1.0);
    }
    case ViewportChannel.Location:
    {
        return float4(surface.getLocation(), 1.0);
    }
    case ViewportChannel.Normal:
    {
        return float4(surface.getNormal(), 1.0);
    }
    case ViewportChannel.RoughnessMetallicSpecular:
    {
        return float4(surface.getRoughness(),surface.getMetallic(),surface.getSpecular(), 1.0);
    }
    case ViewportChannel.Emissive:
    {
        return float4(float3(surface.getEmissive()),1.0f);
    }
    default:
        return float4(1.0);
    }
}
