#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "simple_rect.glsl"
#include "utils.glsl"

layout(location = 0) out vec2 oUV;

void main()
{
    generateRectVertex(pRect.size, ui.projection, pRect.transform, gl_VertexIndex, gl_Position, oUV);
} 