import rin.utils;
import rin.textures;

enum Mode {
    Primitive,
    Texture,
    Glyph,
    ColorWheel
}

enum Primitive {
    Rectangle,
    Circle,
    QuadraticCurve,
    HermaticCurve
}

struct QuadRenderInfo
{
    // [TextureId,RenderMode,0,0]
    int4 opts;
    float2 size;
    float3x3 transform;
    float4 data1;
    float4 data2;
    float4 data3;
    float4 data4;
};

struct PushConstants
{
    float4x4 projection;
    QuadRenderInfo* quads;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct VSIn {
    int instanceId : SV_InstanceID;
    int vertexId : SV_VertexID;
}

struct VSOut {
    float2 uv : UV;
    int quadIndex : QUAD_INDEX;
    float4 position : SV_Position;
}

struct FSIn {
    float2 uv : UV;
    int quadIndex : QUAD_INDEX;
    float2 coordinate : SV_Position;
}

[shader("vertex")]
VSOut vertex(VSIn input)

{
    VSOut output;
    output.quadIndex = input.instanceId;
    QuadRenderInfo quad = push.quads[input.instanceId];
    generateRectVertex(quad.size, push.projection, quad.transform, input.vertexId, output.position, output.uv);
    return output;
}

float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

float screenPxRange(float2 uv, float2 size)
{
    float2 unitRange = float2(12.0) / size;
    float2 screenTexSize = float2(1.0) / fwidth(uv);
    return max(0.5 * dot(unitRange, screenTexSize), 1.0);
}

[shader("fragment")]
float4 fragment(FSIn input)
    : SV_Target
{
    QuadRenderInfo quad = push.quads[input.quadIndex];
    int mode = quad.opts.x;
    switch (mode)
    {
    case 0: // Primitive Shape
    {
        return applyBorderRadius(input.coordinate, quad.data1, quad.data2, quad.size, quad.transform);
    }
    case 1: // Texture
    {
        float4 tint = quad.data1;
        float4 uvMapping = quad.data2;
        float u = mapRangeUnClamped(input.uv.x, 0.0, 1.0, uvMapping.x, uvMapping.z);
        float v = mapRangeUnClamped(input.uv.y, 0.0, 1.0, uvMapping.y, uvMapping.w);
        float2 uv = float2(u, v);

        int textureId = quad.opts.y;

        float4 color = sampleTexture(textureId, uv) * tint;

        return applyBorderRadius(input.coordinate, color, quad.data3, quad.size, quad.transform);
    }
    case 2: // MTSDF Glyph
    {
        float4 uvMapping = quad.data2;
        float u = mapRangeUnClamped(input.uv.x, 0.0, 1.0, uvMapping.x, uvMapping.z);
        float v = mapRangeUnClamped(input.uv.y, 0.0, 1.0, uvMapping.y, uvMapping.w);
        float2 uv = float2(u, v);

        int textureId = quad.opts.y;

        float2 texSize = getTextureSize(textureId);
        float2 actualTexSize = texSize * (uvMapping.zw - uvMapping.xy);
        float3 msd = sampleTexture(textureId, uv).rgb;
        float sd = median(msd.r, msd.g, msd.b);
        float distance = screenPxRange(input.uv, actualTexSize) * (sd - 0.5);
        float opacity = clamp(distance + 0.5, 0.0, 1.0);

        float4 color = quad.data1;

        return lerp(color, float4(color.rgb, 0.0), opacity);
    }
    case 3: // Color wheel
    {
        float2 uv = (input.uv * 2.0f) - 1.0f;

        float r = sqrt((uv.x * uv.x) + (uv.y * uv.y));
        float theta = atan2(uv.y, uv.x);
        float pi = radians(180);
        theta = ((theta + pi) / (pi * 2));
        float4 rgbColor = float4(hsv2rgb(float3(theta, r, 1.0)), 1.0);
        return applyBorderRadius(input.coordinate, rgbColor, float4(quad.size.x / 2.0), quad.size, quad.transform);
    }
    default:
        return float4(1.0);
    }
}
