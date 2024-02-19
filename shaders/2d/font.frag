#version 450

#extension GL_GOOGLE_include_directive : require
#include "font.glsl"

layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

void main() 
{

    FontChar char = font.chars[pFont.idx];
    // sampler2D atlas = AtlasT[char.atlas];
    vec4 uv = char.uv;
    vec2 offset = char.extras.xy;
	vec2 mappedUV = vec2(mapRangeUnClamped(iUV.x,0.0,1.0,uv.x,uv.z),mapRangeUnClamped(iUV.y,0.0,1.0,uv.y,uv.w));
    vec3 msd = texture(AtlasT,mappedUV).rgb;
    // vec3 color = texture(AtlasT,mappedUV).xyz;

    // if(sdf.x < 0.5){
    //     discard;
    // }
    vec4 bgColor = vec4(0.0);
    vec4 fgColor = vec4(1.0);

    float sd = median(msd.r, msd.g, msd.b);
    float screenPxDistance = (4.5)*(sd - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
    oColor = mix(bgColor, fgColor, opacity);
}