layout (location = 0) in vec3 iSceneNormal;
layout (location = 1) in vec2 iUV;
layout (location = 2) in vec3 iSceneLocation;

layout(location = 0) out vec4 oColor;
layout(location = 1) out vec4 oLocation;
layout(location = 2) out vec4 oNormal;
layout(location = 3) out vec4 oRoughMetallic;
layout(location = 4) out vec4 oSpecular;
layout(location = 5) out vec4 oEmissive;


void setOutput(vec3 color, vec3 normal, float roughness, float metallic, vec3 specular, vec3 emissive){
    oColor = vec4(color, 1.0);
    oLocation = vec4(iSceneLocation, 1.0);
    oNormal = vec4(iSceneNormal, 1.0);
    oRoughMetallic = vec4(roughness, metallic, 0.0, 0.0);
    oSpecular = vec4(specular, 0.0);
    oEmissive = vec4(emissive, 0.0);
}