#version 450

#extension GL_GOOGLE_include_directive : require
#include "font.glsl"

layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

layout(set = 1, binding = 2) uniform options{   
	vec4 color;
} opts;

void main() 
{
    vec4 bgColor = vec4(0.0);
    vec4 fgColor = opts.color;

    FontChar char = font.chars[int(pFont.info.x)];
    int atlasIdx = int(char.info.x);
    vec3 msdf = texture(AtlasT[atlasIdx],iUV).rgb;
    ivec2 sz = textureSize(AtlasT[atlasIdx], 0).xy;
    float dx = dFdx(iUV.x) * sz.x; 
    float dy = dFdy(iUV.y) * sz.y;
    float toPixels = 12.0 * inversesqrt(dx * dx + dy * dy);
    float sigDist = median(msdf.r, msdf.g, msdf.b);
    float w = fwidth(sigDist);
    float opacity = smoothstep(0.5 - w, 0.5 + w, sigDist);
    oColor = mix(bgColor, fgColor, opacity);
}