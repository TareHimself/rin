namespace InterfaceDispatchCheck
{
    struct Gold
    {
        float3 tint;
        float roughness;
    }

    struct Surface
    {
        float3 color;
        float roughness;
    }

    struct SurfacePush_Gold
    {
        Gold* materials;
        Surface* output;
    }

}

using namespace InterfaceDispatchCheck;

extension InterfaceDispatchCheck::Gold
{
    static Surface evaluate(in Gold material, float2 uv)
    {
        Surface surface;
        surface.color = material.tint * uv.x;
        surface.roughness = material.roughness;
        return surface;
    }
}

[[vk::push_constant]] uniform ConstantBuffer<SurfacePush_Gold, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    push.output[0] = Gold.evaluate(push.materials[0], float2(0.5));
}
