#version 450

#extension GL_GOOGLE_include_directive : require

#include "pbr.frag"


layout(set = 1, binding = 0) uniform sampler2D ColorT;
layout(set = 1, binding = 1) uniform sampler2D NormalT;
layout(set = 1, binding = 2) uniform sampler2D RoughnessT;
layout(set = 1, binding = 3) uniform sampler2D MetallicT;
layout(set = 1, binding = 4) uniform sampler2D SpecularT;
layout(set = 1, binding = 5) uniform sampler2D EmissiveT;

layout (location = 0) out vec4 oColor;

void main()
{
    vec3 viewDir = normalize(scene.cameraLocation.xyz - iSceneLocation);

    vec3 normal = applyNormalMap(NormalT, normalize(iSceneNormal), viewDir, iUV);
    //inWorldLocation.xy
    vec3 color = texture(ColorT, iUV).xyz;

    vec3 roughness = texture(RoughnessT, iUV).xyz;
    //oColor = vec4(color,1.0);
    // //vec3 color = texture(inColorTexture,inWorldLocation.xy).xyz;
    // vec3 ambient = color * scene.ambientColor.xyz;
    //vec4(color,1.0f);//
    oColor = vec4(computeColor(color, normal, roughness.r, 1.0, vec3(0.0, 0.0, 0.0), vec3(0.0)), 1.0);//vec4(color * lightValue *  scene.sunlightColor.w + ambient ,1.0f);
}