
#include "mesh_vert.ash"

@Fragment {

    #include "usage.ash"
   
    void main()
    {
        setOutput(float3(1.0), iSceneNormal, 0.5, 1.0, 0.0,float3(0.0));
    }
}