#version 450

#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_buffer_reference : require

#include "normal.glsl"
#include "scene.glsl"
#include "deferred.glsl"


layout(set = 1, binding = 0) uniform sampler2D ColorT;
layout(set = 1, binding = 1) uniform sampler2D NormalT;
layout(set = 1, binding = 2) uniform sampler2D RoughnessT;
layout(set = 1, binding = 3) uniform sampler2D MetallicT;
layout(set = 1, binding = 4) uniform sampler2D SpecularT;
layout(set = 1, binding = 5) uniform sampler2D EmissiveT;

void main() 
{
    vec3 viewDir = normalize(scene.cameraLocation.xyz - iSceneLocation);

	vec3 normal = applyNormalMap(NormalT,normalize(iSceneNormal),viewDir,iUV);

	vec3 color = texture(ColorT,iUV).xyz;

	vec3 roughness = texture(RoughnessT,iUV).xyz;

	setOutput(color,normal,roughness.r,1.0,vec3(0.0),vec3(0.0));
}