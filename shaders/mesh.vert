#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "scene.glsl"

layout (location = 0) out vec3 oSceneNormal;
layout (location = 1) out vec2 oUV;
layout (location = 2) out vec3 oSceneLocation;

struct Vertex {
	vec4 location;
	vec4 normal;
	vec4 uv;
}; 

layout(buffer_reference, std430) readonly buffer VertexBuffer{ 
	Vertex vertices[];
};

//push constants block
layout( push_constant ) uniform constants
{
	mat4 transformMatrix;
	VertexBuffer vertexBuffer;
} vertexPushConstant;

void main() 
{
	Vertex v = vertexPushConstant.vertexBuffer.vertices[gl_VertexIndex];
	
	vec4 location = vec4(v.location.xyz, 1.0f);

	mat4 viewProjection = scene.projectionMatrix * scene.viewMatrix;

	vec4 scenePositon =  viewProjection * vertexPushConstant.transformMatrix * location;

	gl_Position = scenePositon;

	oSceneNormal = (vertexPushConstant.transformMatrix * vec4(v.normal.xyz, 0.f)).xyz;

	oUV = v.uv.xy;

	oSceneLocation = scenePositon.xyz;
}