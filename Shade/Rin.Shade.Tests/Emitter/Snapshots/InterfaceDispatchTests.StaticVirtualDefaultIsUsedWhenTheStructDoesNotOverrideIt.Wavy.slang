namespace InterfaceDispatchCheck
{
    struct Wavy
    {
        float amplitude;
    }

    struct OffsetPush_Wavy
    {
        Wavy* materials;
        float3* output;
    }

}

using namespace InterfaceDispatchCheck;

extension InterfaceDispatchCheck::Wavy
{
    static float3 offset(in Wavy material, float3 world)
    {
        return float3(0, material.amplitude * world.x, 0);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<OffsetPush_Wavy, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var world = float3(1, 2, 3);
    push.output[0] = world + Wavy.offset(push.materials[0], world);
}
