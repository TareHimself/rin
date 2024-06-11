
#include "mesh_vert.ash"

@Fragment {

    #include "deferred_usage.ash"
   
    void main()
    {
        float3 viewDir = normalize(scene.cameraLocation.xyz - iSceneLocation);

        float3 normal = iSceneNormal;

        float3 color = float3(1.0,0.0,0.0);

        setOutput(iSceneLocation, normal, 0.5, 1.0, 0.0,float3(0.0));
    }
}