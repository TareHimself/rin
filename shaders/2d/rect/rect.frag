#version 450

#extension GL_GOOGLE_include_directive : require

layout (location = 0) in vec4 iColor;
layout (location = 0) out vec4 oColor;

void main() 
{
	oColor = iColor;
}