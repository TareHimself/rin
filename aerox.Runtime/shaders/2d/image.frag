#version 450

#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "rect.glsl"
#include "utils.glsl"

layout(set = 1, binding = 1) uniform sampler2D ImageT;
layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

layout(set = 1, binding = 0, scalar) uniform options{
    vec4 tint;// the image tint
    int bHasTexture;// check if we have a texture
    vec4 borderRadius;
} opts;

void main() {
//    if (shouldDiscard(ui.viewport, pRect.clip, gl_FragCoord.xy)){
//        discard;
//    }

    vec4 pxColor = opts.tint;

    if (opts.bHasTexture == 1){
        pxColor = pxColor * texture(ImageT, iUV);
    }

    oColor = applyBorderRadius(gl_FragCoord.xy, pxColor,opts.borderRadius,pRect.size,pRect.transform);
}