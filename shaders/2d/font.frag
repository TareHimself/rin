#version 450

#extension GL_GOOGLE_include_directive : require
#include "font.glsl"

layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;


void main() 
{

    FontChar char = font.chars[pFont.idx];
    // sampler2D atlas = AtlasT[char.atlas];
    vec4 uv = char.uv;
    vec2 offset = char.extras.xy;
    int atlas = int(char.extras.w);
	vec2 mappedUV = vec2(mapRangeUnClamped(iUV.x,0.0,1.0,uv.x,uv.y),mapRangeUnClamped(iUV.y,0.0,1.0,uv.z,uv.w));
    vec3 color = texture(AtlasT[atlas],mappedUV).xyz;

    if(color.x < 0.7){
        discard;
    }

	oColor = vec4(color,0.0);
}