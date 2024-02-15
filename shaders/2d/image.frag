#version 450

#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "rect.glsl"

layout(set = 1, binding = 0) uniform sampler2D ImageT;
layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;


void main() {
    if(shouldDiscard(ui.viewport,pRect.clip,gl_FragCoord.xy)){
        discard;
    }
	//vec2 uv = vec2(mapRangeUnClamped(iUV.x,0.0,1.0,0.5,1.0),mapRangeUnClamped(iUV.y,0.0,1.0,0.5,1.0));
	oColor = texture(ImageT,iUV);
}