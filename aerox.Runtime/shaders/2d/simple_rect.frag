#version 450

#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "simple_rect.glsl"
#include "utils.glsl"

layout(set = 1, binding = 1) uniform sampler2D ImageT;
layout (location = 0) in vec2 iUV;
layout (location = 0) out vec4 oColor;

void main() {
//    if (shouldDiscard(ui.viewport, pRect.clip, gl_FragCoord.xy)){
//        discard;
//    }

    vec4 pxColor = pRect.color;

    oColor = applyBorderRadius(gl_FragCoord.xy, pxColor,pRect.borderRadius,pRect.size,pRect.transform);
}