#include "Framework/Shaders/Attributes/blending.slang"
#include "Framework/Shaders/utils.slang"

struct Data {
    float4x4 projection;
    float2 screenSize;
    float4x4 transform;
    float2 size;
    float time;
    float2 cursor;
}

struct PushConstants
{
    Data *data;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int instanceId: SV_InstanceID, int vertexId: SV_VertexID, out float2 oUV: UV)
    : SV_Position
{
    float4 position;
    generateRectVertex(push.data->size, push.data->projection, push.data->transform, vertexId, position, oUV);
    return position;
}

// https://iquilezles.org/articles/palettes/
float3 palette(float t) {
    float3 a = float3(0.5, 0.5, 0.5);
    float3 b = float3(0.5, 0.5, 0.5);
    float3 c = float3(1.0, 1.0, 1.0);
    float3 d = float3(0.263, 0.416, 0.557);
    // float3 a = PushConstants.data1.rgb;
    // float3 b = PushConstants.data2.rgb;
    // float3 c = PushConstants.data3.rgb;
    // float3 d = PushConstants.data3.rgb;

    return a + b * cos(6.28318 * (c * t + d));
}

// https://www.shadertoy.com/view/mtyGWy
[shader("fragment"),BlendUI,Stencil,Attachment(AttachmentFormat.RGBA32)]
float4 fragment(in float2 iUV: UV, float2 coordinate: SV_Position)
    : SV_Target
{
    float2 fragCoord = coordinate;
    int2 iResolution = int2((int)push.data->size.x, (int)push.data->size.y);
    float iTime = push.data->time;

    float2 size = push.data->size;
    float2 screenUVS = fragCoord.xy / size.xy;
    float2 uv = screenUVS; //((iUV * 2.0) - 1.0) + topLeft;//(((fragCoord - topLeft) * 2.0 - iResolution.xy) / iResolution.y);
    float2 cursor = push.data->cursor;
    float2 screen = push.data->screenSize;
    uv = uv - float2(mapRangeUnClamped(cursor.x, 0.0, size.x, 0.0, 1.0), mapRangeUnClamped(cursor.y, 0.0, size.y, 0.0, 1.0));
    float2 uv0 = uv;

    float3 finalColor = float3(0.0);

    for (float i = 0.0; i < 4.0; i++) {
        uv = fract(uv * 1.5) - 0.5;

        float d = length(uv) * exp(length(uv0) * -1.0);

        float3 col = palette(length(uv0) + i * 0.4 + iTime * 0.4);

        d = sin(d * 8.0 + iTime) / 8.0;
        d = abs(d);

        d = pow(0.01 / d, 1.2);

        finalColor += col * d;
    }

    return float4(finalColor, 1.0);
}