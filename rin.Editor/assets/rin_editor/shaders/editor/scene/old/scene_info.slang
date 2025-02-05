import rin.scene.light;

struct SceneInfo
{
    float4x4 view;
    float4x4 projection;
    float4x4 viewProjection;
    PackedLight *lights;
    int numLights;
}