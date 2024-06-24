
#include "mesh_vert.ash"

@Fragment {

    #include "usage.ash"
    #include "normal.ash"
    
    layout(set = 1, binding = 0) uniform sampler2D TColor;
    layout(set = 1, binding = 1) uniform sampler2D TNormal;
    layout(set = 1, binding = 2) uniform sampler2D TRoughness;
    layout(set = 1, binding = 3) uniform sampler2D TMetallic;
    
    void main()
    {
        float3 viewDir = normalize(scene.cameraLocation.xyz - iSceneLocation);

        float3 normal = applyNormalMap(TNormal, normalize(iSceneNormal), viewDir, iUV);

        float3 color = texture(TColor, iUV).xyz;
        float roughness = texture(TRoughness, iUV).x;
        float metallic = texture(TMetallic, iUV).x;

        setOutput(color, normal, roughness,metallic, 0.0,float3(0.0));
        //etOutput(color, iSceneNormal, 0.5, 1.0, 0.0,float3(0.0));
    }
}