
#include "mesh_vert.ash"
#include "normal.ash"

@Fragment {

    #include "deferred_usage.ash"
    
    layout(set = 1, binding = 0) uniform sampler2D ColorT;
    layout(set = 1, binding = 1) uniform sampler2D NormalT;
    layout(set = 1, binding = 2) uniform sampler2D RoughnessT;
    layout(set = 1, binding = 3) uniform sampler2D MetallicT;
    layout(set = 1, binding = 4) uniform sampler2D SpecularT;
    layout(set = 1, binding = 5) uniform sampler2D EmissiveT;

    void main()
    {
        float3 viewDir = normalize(scene.cameraLocation.xyz - iSceneLocation);

        float3 normal = applyNormalMap(NormalT, normalize(iSceneNormal), viewDir, iUV);

        float3 color = texture(ColorT, iUV).xyz;

        float3 roughness = texture(RoughnessT, iUV).xyz;

        setOutput(color, normal, roughness.r, 1.0, 0.0,float3(0.0));
    }
}