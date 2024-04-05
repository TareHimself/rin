#version 450

#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "rect.glsl"

layout(set = 1, binding = 0) uniform sampler2D ImageT;
layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

layout(set = 1, binding = 1) uniform options{   
	vec4 tint; // the image tint
	int bHasTexture; // check if we have a texture
    float borderRadius;
} opts;

void main() {
    if(shouldDiscard(ui.viewport,pRect.clip,gl_FragCoord.xy)){
        discard;
    }

    vec4 pxColor = vec4(1.0);

    if(opts.bHasTexture == 1){
        pxColor = texture(ImageT,iUV);
    }

	//vec2 uv = vec2(mapRangeUnClamped(iUV.x,0.0,1.0,0.5,1.0),mapRangeUnClamped(iUV.y,0.0,1.0,0.5,1.0));
	oColor = opts.tint * pxColor;
}