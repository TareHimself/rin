#version 450

#extension GL_GOOGLE_include_directive : require
#include "font.glsl"

layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

layout(set = 1, binding = 1, scalar) uniform OptionsUniform{
    vec4 bg;
    vec4 fg;
} options;

float screenPxRange(vec2 uv) {
    vec2 unitRange = vec2(15)/vec2(textureSize(TAtlas[pFont.textureIdx], 0));
    vec2 screenTexSize = vec2(1.0)/fwidth(uv);
    return max(0.5*dot(unitRange, screenTexSize), 1.0);
}

void main()
{
    vec4 bgColor = options.bg;
    vec4 fgColor = options.fg;
    vec2 uv = iUV;
    uv.y = 1 - uv.y;
    //oColor = texture(TAtlas[pFont.textureIdx], uv);

    vec3 msd = texture(TAtlas[pFont.textureIdx], uv).rgb;
    float sd = median(msd.r, msd.g, msd.b);
    float screenPxDistance = screenPxRange(uv)*(sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    oColor = mix(fgColor, bgColor, opacity);
}