#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "ui.glsl"
#include "simple_rect.glsl"
#include "utils.glsl"

#define PI 3.14159265359

layout(location = 0) out vec2 oUV;

mat2 rotate2d(float _angle){
    return mat2(cos(_angle),-sin(_angle),
    sin(_angle),cos(_angle));
}

void main()
{
    generateRectVertex(pRect.size,ui.projection,pRect.transform,gl_VertexIndex,gl_Position,oUV);
}