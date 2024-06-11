layout (location = 0) in float3 iSceneNormal;
layout (location = 1) in float2 iUV;
layout (location = 2) in float3 iSceneLocation;

layout(location = 0) out float4 oColor;
layout(location = 1) out float4 oLocation;
layout(location = 2) out float4 oNormal;
layout(location = 3) out float4 oRoughMetallicSpecular;
layout(location = 4) out float4 oSpecular;
layout(location = 5) out float4 oEmissive;


void setOutput(float3 color, float3 normal, float roughness, float metallic, float specular, float3 emissive){
    oColor = float4(color, 1.0);
    oLocation = float4(iSceneLocation, 1.0);
    oNormal = float4(iSceneNormal, 1.0);
    oRoughMetallicSpecular = float4(roughness, metallic, specular, 0.0);
    oEmissive = float4(emissive, 0.0);
}