#include "Framework/Shaders/utils.slang"
#include "Framework/Shaders/sd.slang"
#include "Framework/Shaders/images.slang"
#include "Framework/Shaders/type_alias.slang"
#include "Framework/Shaders/Attributes/blending.slang"
enum QuadMode
{
    Line,
    Circle,
    Rectangle,
    QuadraticCurve,
    CubicCurve,
    Texture,
    Mtsdf,
    ColorWheel
}

enum PrimitiveType
{
    
}

struct Quad
{
    // [RenderMode,Reserved,Reserved,Reserved]
    int4 opts;
    float2 size;
    float4x4 transform;
}

struct CompleteQuad {
    Quad super;
    // 128 bytes worth of data per quad
    float4 data[8];
}

struct Primitive {
    Quad super;
    PrimitiveType type;
    float4 data[4];
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

struct LineData
{
    Quad super;
    Color color;
    Vector2 begin;
    Vector2 end;
    float thickness;
}

struct CircleData
{
    Quad super;
    float4x4 inverseTransform;
    Color color;
    float radius;
}

struct RectangleData
{
    Quad super;
    float4x4 inverseTransform;
    Color color;
    Vector4 borderRadius;
}

struct QuadraticCurveData
{
    Quad super;
    Color color;
    Vector2 begin;
    Vector2 end;
    Vector2 control;
    float thickness;
}

struct CubicCurveData
{
    Quad super;
    Color color;
    Vector2 begin;
    Vector2 end;
    Vector2 controlA;
    Vector2 controlB;
    float thickness;
}

struct PushConstants
{
    float4x4 projection;
    float4 viewport;
    CompleteQuad *quads;
}

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

#define SDF_HALF_STEP 1.0

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
    case QuadMode.Line:
    {
        var data = reinterpret<LineData>(quad);

        var d = sdSegment(input.coordinate, data.begin, data.end) - (data.thickness * 0.5);

        // var w = fwidth(input.uv);
        float w = max(fwidth(d), 1e-6);

        var wLength = length(w);

        var aaWidth = 0.5 * wLength;

        if (d > aaWidth) {
            discard;
        }

        float alpha = smoothstep(-aaWidth, aaWidth, d);

        var color = data.color;

        return lerp(color, float4(color.rgb, 0.0), alpha);
    }
    case QuadMode.Circle:
    {

        var data = reinterpret<CircleData>(quad);

        var d = sdCircle((data.inverseTransform * input.coordinate), float2(data.radius), data.radius);

        // var w = fwidth(input.uv);
        float w = max(fwidth(d), 1e-6);
        
        var wLength = length(w);

        var aaWidth = 0.5 * wLength;

        if (d > aaWidth) {
            discard;
        }

        float alpha = smoothstep(-aaWidth,aaWidth, d);

        var color = data.color;

        return lerp(color, float4(color.rgb, 0.0), alpha);
    }
    case QuadMode.Rectangle:
    {

        var data = reinterpret<RectangleData>(quad);

        var d = sdRoundedBox((data.inverseTransform * input.coordinate),quad.super.size / 2.0, quad.super.size / 2.0, data.borderRadius);

        var w = max(fwidth(d), 1e-6);

        var wLength = length(w);

        var aaWidth = 0.5 * wLength;

        if (d > aaWidth) {
            discard;
        }

        float alpha = smoothstep(-aaWidth, aaWidth, d);

        var color = data.color;

        return lerp(color, float4(color.rgb, 0.0), alpha);
    }
    case QuadMode.QuadraticCurve:
    {
        var data = reinterpret<QuadraticCurveData>(quad);

        var d = sdBezier(input.coordinate, data.begin, data.end, data.control) - (data.thickness * 0.5);

        float w = max(fwidth(d), 1e-6);

        var wLength = length(w);

        var aaWidth = 0.5 * wLength;

        if (d > aaWidth) {
            discard;
        }

        float alpha = smoothstep(-aaWidth, aaWidth, d);

        var color = data.color;

        return lerp(color, float4(color.rgb, 0.0), alpha);
    }
    case QuadMode.CubicCurve:
    {
        var data = reinterpret<CubicCurveData>(quad);

        var d = sdCubic(input.coordinate, data.begin, data.end, data.controlA, data.controlB) - (data.thickness * 0.5);

        float w = max(fwidth(d), 1e-6);

        var wLength = length(w);

        var aaWidth = 0.5 * wLength;

        if (d > aaWidth) {
            discard;
        }

        float alpha = smoothstep(-aaWidth, aaWidth, d);

        var color = data.color;

        return lerp(color, float4(color.rgb, 0.0), alpha);
    }
    // case QuadMode.Primitive: // Primitive Shape
    // {
    //     var asPrimitive = reinterpret<Primitive>(quad);
    //     switch (asPrimitive.type) {
    //     case PrimitiveType.Line:
    //     {
    //         var a = asPrimitive.data2.xy;
    //         var b = asPrimitive.data2.zw;

    //         var d = sdSegment(input.coordinate, a, b);

    //         float thickness = asPrimitive.data3.x; // Assuming thickness is stored in data3.z
    //         float smoothedAlpha = smoothstep(thickness - SDF_HALF_STEP, thickness, d);
    //         // float smoothedAlpha = smoothstep(0.0f, 1.0f, d);
    //         // return float4(float3(abs(d),0.0f,0.0f),1.0f);
    //         return float4(asPrimitive.data1.rgb, 1 - smoothedAlpha);
    //     }
    //     case PrimitiveType.Circle:
    //     {
    //         var center = size / 2.0f;
    //         center = transform * center;
    //         var d = sdCircle(input.coordinate, center, asPrimitive.data2.x);
    //         float smoothedAlpha = smoothstep(-SDF_HALF_STEP, 0.0f, d);
    //         return lerp(float4(asPrimitive.data1.rgb,1.0),float4(asPrimitive.data1.rgb,0.0),smoothedAlpha);
    //     }
    //     case PrimitiveType.Rectangle:
    //         return applyBorderRadius(input.coordinate, asPrimitive.data1, asPrimitive.data2, size, transform,SDF_HALF_STEP);
    //     case PrimitiveType.QuadraticCurve:
    //     {
    //         var a = asPrimitive.data2.xy;
    //         var b = asPrimitive.data2.zw;
    //         var C = asPrimitive.data3.xy;

    //         var d = sdBezier(input.coordinate, a, b, C);
    //         float thickness = asPrimitive.data3.z; // Assuming thickness is stored in data3.z
    //         float smoothedAlpha = smoothstep(thickness - SDF_HALF_STEP, thickness, d);
    //         // float smoothedAlpha = smoothstep(0.0f, 1.0f, d);
    //         // return float4(float3(abs(d),0.0f,0.0f),1.0f);
    //         return float4(asPrimitive.data1.rgb, 1 - smoothedAlpha);
    //     }
    //     case PrimitiveType.CubicCurve:
    //     {
    //         var a = asPrimitive.data2.xy;
    //         var b = asPrimitive.data2.zw;
    //         var aC = asPrimitive.data3.xy;
    //         var bC = asPrimitive.data3.zw;

    //         var d = sdBezierCubic(input.coordinate, a, b, aC, bC);
    //         float thickness = asPrimitive.data3.z; // Assuming thickness is stored in data3.z
    //         float smoothedAlpha = smoothstep(thickness - SDF_HALF_STEP, thickness, d);
    //         // float smoothedAlpha = smoothstep(0.0f, 1.0f, d);
    //         // return float4(float3(abs(d),0.0f,0.0f),1.0f);
    //         return lerp(asPrimitive.data1, float4(asPrimitive.data1.rgb, 0.0), smoothedAlpha);
    //     }
    //     }
    // }
    case QuadMode.Texture: // Texture
    {
        var asTexture = reinterpret<Texture>(quad);
        float4 uvMapping = asTexture.uv;
        var uv = mapRangeUnClamped(input.uv, float2(0.0), float2(1.0), uvMapping.xy, uvMapping.zw);
        var textureHandle = asTexture.textureHandle;

        float4 color = textureHandle.SampleTexture(uv) * asTexture.tint;

        return applyBorderRadius(input.coordinate, color, asTexture.borderRadius, size, transform);
    }
    case QuadMode.Mtsdf: // MTSDF Mtsdf
    {
        
        var asMtsdf = reinterpret<Mtsdf>(quad);
        float4 uvMapping = asMtsdf.uv;
        var uv = mapRangeUnClamped(input.uv, float2(0.0), float2(1.0), uvMapping.xy, uvMapping.zw);
        var textureHandle = asMtsdf.textureHandle;

        float3 msd = textureHandle.SampleTexture(uv,ImageTiling.ClampEdge).rgb;
        float2 texSize = getTextureSize(textureHandle);
        float2 actualTexSize = texSize * (uvMapping.zw - uvMapping.xy);
        
        float sd = median(msd.r, msd.g, msd.b);

        float distance = screenPxRange(uv, actualTexSize) * (sd - 0.5);

        float w = max(fwidth(distance), 1e-6);

        var wLength = length(w);

        var aaWidth = 0.5 * wLength;

        float opacity = 1.0 - clamp(distance + 0.5, 0.0, 1.0);

        float4 color = asMtsdf.color;

        return float4(color.rgb, color.a * opacity);
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
