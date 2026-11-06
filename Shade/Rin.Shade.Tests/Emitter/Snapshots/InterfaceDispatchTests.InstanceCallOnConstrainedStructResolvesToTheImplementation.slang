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
    Surface evaluate(float2 uv)
    {
        Surface surface;
        surface.color = this.tint * uv.x;
        surface.roughness = this.roughness;
        return surface;
    }
}

[[vk::push_constant]] uniform ConstantBuffer<SurfacePush_Gold, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var material = push.materials[0];
    push.output[0] = material.evaluate(float2(0.5));
}
