#version 450

#extension GL_GOOGLE_include_directive : require
#include "font.glsl"
#include "utils.glsl"

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
    vec2 unitRange = vec2(15)/vec2(pFont.rect.zw);
    vec2 screenTexSize = vec2(1.0)/fwidth(uv);
    return max(0.5*dot(unitRange, screenTexSize), 1.0);
}

void main()
{
    
    vec4 bgColor = options.bg;
    vec4 fgColor = options.fg;
    
    
    vec2 uv = iUV;

    vec2 atlasSize = vec2(textureSize(TAtlas[pFont.atlasIdx], 0));
    
    vec4 rectInAtlas = vec4(vec2(pFont.rect.xy) / atlasSize,vec2(pFont.rect.xy + pFont.rect.zw) / atlasSize);
    
    vec2 actualUv = vec2(mapRangeUnClamped(uv.x,0.0,1.0,rectInAtlas.x,rectInAtlas.z),mapRangeUnClamped(uv.y,1.0,0.0,rectInAtlas.y,rectInAtlas.w));
    
    vec3 msd = texture(TAtlas[pFont.atlasIdx], actualUv).rgb;
    
    float sd = median(msd.r, msd.g, msd.b);
    
    float screenPxDistance = screenPxRange(uv)*(sd - 0.5);
    
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

    oColor = mix(fgColor, bgColor, opacity);
}