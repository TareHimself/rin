#include "Engine/Shaders/utils.slang"
#include "Engine/Shaders/sd.slang"
#include "Engine/Shaders/images.slang"
#include "Engine/Shaders/Attributes/blending.slang"
enum QuadMode
{
    Primitive,
    Texture,
    Mtsdf,
    ColorWheel
}

enum PrimitiveType
{
    Line,
    Circle,
    Rectangle,
    QuadraticCurve,
    CubicCurve
}

struct Quad
{
    // [RenderMode,Reserved,Reserved,Reserved]
    int4 opts;
    float2 size;
    float4x4 transform;
};

struct CompleteQuad {
    Quad super;
    // 128 bytes worth of data per quad
    float4 data[8];
}

struct PushConstants
{
    float4x4 projection;
    float4 viewport;
    CompleteQuad *quads;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct VSIn
{
    int instanceId : SV_InstanceID;
    int vertexId : SV_VertexID;
}

struct VSOut
{
    float2 uv : UV;
    int quadIndex : QUAD_INDEX;
    float4 position : SV_Position;
}

struct FSIn
{
    float2 uv : UV;
    int quadIndex : QUAD_INDEX;
    float2 coordinate : SV_Position;
}

[shader("vertex")]
VSOut vertex(VSIn input)

{
    VSOut output;
    output.quadIndex = input.instanceId;
    var quad = reinterpret<Quad>(push.quads[input.instanceId]);
    generateRectVertex(quad.size, push.projection, quad.transform, input.vertexId, output.position, output.uv);
    return output;
}

float median(float r, float g, float b)
{
    return max(min(r, g), min(max(r, g), b));
}

float screenPxRange(float2 uv, float2 size)
{
    // return (max(size.x, size.y) / 32.0f) * 6.0f;
    float2 unitRange = float2(12.0) / size;
    float2 screenTexSize = float2(1.0) / fwidth(uv);
    return max(0.5 * dot(unitRange, screenTexSize), 1.0);
}

struct Primitive {
    Quad super;
    PrimitiveType type;
    float4 data1;
    float4 data2;
    float4 data3;
    float4 data4;
}

struct Texture {
    Quad super;
    ImageHandle textureHandle;
    float4 tint;
    float4 uv;
    float4 borderRadius;
}

struct Mtsdf {
    Quad super;
    ImageHandle textureHandle;
    float4 color;
    float4 uv;
}

#define SDF_HALF_STEP 0.5

[shader("fragment"),Attachment(AttachmentFormat.RGBA32), BlendUI,Stencil]
float4 fragment(FSIn input)
    : SV_Target
    {
    var quad = push.quads[input.quadIndex];
    var mode = (QuadMode)quad.super.opts.x;
    var transform = quad.super.transform;
    var size = quad.super.size;
    switch (mode)
    {
    case QuadMode.Primitive: // Primitive Shape
    {
        var asPrimitive = reinterpret<Primitive>(quad);
        switch (asPrimitive.type) {
        case PrimitiveType.Line:
        {
            var a = asPrimitive.data2.xy;
            var b = asPrimitive.data2.zw;

            var d = sdSegment(input.coordinate, a, b);

            float thickness = asPrimitive.data3.x; // Assuming thickness is stored in data3.z
            float smoothedAlpha = smoothstep(thickness - SDF_HALF_STEP, thickness + SDF_HALF_STEP, d);
            // float smoothedAlpha = smoothstep(0.0f, 1.0f, d);
            // return float4(float3(abs(d),0.0f,0.0f),1.0f);
            return float4(asPrimitive.data1.rgb, 1 - smoothedAlpha);
        }
        case PrimitiveType.Circle:
        {
            var d = sdCircle(input.coordinate, asPrimitive.data2.x);
            float smoothedAlpha = smoothstep(-0.5, 0.5, d);
            return float4(asPrimitive.data1.rgb, 1 - smoothedAlpha);
        }
        case PrimitiveType.Rectangle:
            return applyBorderRadius(input.coordinate, asPrimitive.data1, asPrimitive.data2, size, transform);
        case PrimitiveType.QuadraticCurve:
        {
            var a = asPrimitive.data2.xy;
            var b = asPrimitive.data2.zw;
            var C = asPrimitive.data3.xy;

            var d = sdBezier(input.coordinate, a, b, C);
            float thickness = asPrimitive.data3.z; // Assuming thickness is stored in data3.z
            float smoothedAlpha = smoothstep(thickness - SDF_HALF_STEP, thickness + SDF_HALF_STEP, d);
            // float smoothedAlpha = smoothstep(0.0f, 1.0f, d);
            // return float4(float3(abs(d),0.0f,0.0f),1.0f);
            return float4(asPrimitive.data1.rgb, 1 - smoothedAlpha);
        }
        case PrimitiveType.CubicCurve:
        {
            var a = asPrimitive.data2.xy;
            var b = asPrimitive.data2.zw;
            var aC = asPrimitive.data3.xy;
            var bC = asPrimitive.data3.zw;

            var d = sdBezierCubic(input.coordinate, a, b, aC, bC);
            float thickness = asPrimitive.data3.z; // Assuming thickness is stored in data3.z
            float smoothedAlpha = smoothstep(thickness - SDF_HALF_STEP, thickness + SDF_HALF_STEP, d);
            // float smoothedAlpha = smoothstep(0.0f, 1.0f, d);
            // return float4(float3(abs(d),0.0f,0.0f),1.0f);
            return lerp(asPrimitive.data1, float4(asPrimitive.data1.rgb, 0.0), smoothedAlpha);
        }
        }
    }
    case QuadMode.Texture: // Texture
    {
        var asTexture = reinterpret<Texture>(quad);
        float4 uvMapping = asTexture.uv;
        var uv = mapRangeUnClamped(input.uv, float2(0.0), float2(1.0), uvMapping.xy, uvMapping.zw);
        var textureHandle = asTexture.textureHandle;

        float4 color = sampleTexture(textureHandle, uv) * asTexture.tint;

        return applyBorderRadius(input.coordinate, color, asTexture.borderRadius, size, transform);
    }
    case QuadMode.Mtsdf: // MTSDF Mtsdf
    {
        
        var asMtsdf = reinterpret<Mtsdf>(quad);
        float4 uvMapping = asMtsdf.uv;
        var uv = mapRangeUnClamped(input.uv, float2(0.0), float2(1.0), uvMapping.xy, uvMapping.zw);
        var textureHandle = asMtsdf.textureHandle;

        float3 msd = sampleTexture(textureHandle, uv,ImageTiling.ClampEdge).rgb;
        float2 texSize = getTextureSize(textureHandle);
        float2 actualTexSize = texSize * (uvMapping.zw - uvMapping.xy);
        
        // return float4(msd,1.0f);
        float sd = median(msd.r, msd.g, msd.b);

        float distance = screenPxRange(input.uv, actualTexSize) * (sd - 0.5);

        float opacity = clamp(distance + 0.5, 0.0, 1.0);

        float4 color = asMtsdf.color;

        return lerp(color, float4(color.rgb, 0.0), opacity);
    }
    case QuadMode.ColorWheel: // Color wheel
    {
        float2 uv = (input.uv * 2.0f) - 1.0f;

        float r = sqrt((uv.x * uv.x) + (uv.y * uv.y));
        float theta = atan2(uv.y, uv.x);
        float pi = radians(180);
        theta = ((theta + pi) / (pi * 2));
        float4 rgbColor = float4(hsv2rgb(float3(theta, r, 1.0)), 1.0);
        return applyBorderRadius(input.coordinate, rgbColor, float4(size.x / 2.0), size, transform);
    }
    default:
        return float4(1.0);
    }
}
