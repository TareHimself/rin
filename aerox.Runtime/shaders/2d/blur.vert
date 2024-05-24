#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "blur.glsl"
#include "utils.glsl"

layout(location = 0) out vec2 oUV;


void main()
{
    generateRectVertex(push.size,ui.projection,push.transform,gl_VertexIndex,gl_Position,oUV);
}