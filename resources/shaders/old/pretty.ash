#include "rect.rsl"

@Fragment{
    layout (location = 0) in float2 iUV;
    layout (location = 0) out float4 oColor;

    //https://iquilezles.org/articles/palettes/
    float3 palette(float t) {
        float3 a = float3(0.5, 0.5, 0.5);
        float3 b = float3(0.5, 0.5, 0.5);
        float3 c = float3(1.0, 1.0, 1.0);
        float3 d = float3(0.263, 0.416, 0.557);
        // float3 a = PushConstants.data1.rgb;
        // float3 b = PushConstants.data2.rgb;
        // float3 c = PushConstants.data3.rgb;
        // float3 d = PushConstants.data3.rgb;

        return a + b*cos(6.28318*(c*t+d));
    }

    //https://www.shadertoy.com/view/mtyGWy
    void main() {
        int2 fragCoord = int2(gl_FragCoord.xy);
        int2 iResolution = int2(push.size);
        float iTime = ui.time.x;
        float2 topLeft = doProjectionAndTransformation(float2(0.0),  ui.projection, push.transform);
        float2 uv = (((fragCoord - topLeft) * 2.0 - iResolution.xy) / iResolution.y);
        float2 uv0 = uv;

        float3 finalColor = float3(0.0);

        for (float i = 0.0; i < 4.0; i++) {
            uv = fract(uv * 1.5) - 0.5;

            float d = length(uv) * exp(length(uv0) * -1.0);

            float3 col = palette(length(uv0) + i*0.4 + iTime*0.4);

            d = sin(d*8.0 + iTime)/8.0;
            d = abs(d);

            d = pow(0.01 / d, 1.2);

            finalColor += col * d;
        }

        oColor = float4(finalColor, 1.0);

    }
}