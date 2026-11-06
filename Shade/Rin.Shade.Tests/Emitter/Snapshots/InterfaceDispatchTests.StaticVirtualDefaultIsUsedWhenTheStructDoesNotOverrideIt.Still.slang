namespace InterfaceDispatchCheck
{
    struct Still
    {
        float unused;
    }

    struct OffsetPush_Still
    {
        Still* materials;
        float3* output;
    }

}

using namespace InterfaceDispatchCheck;

float3 IVertexOffset_Still_offset(in Still material, float3 world)
{
    return float3(0);
}

[[vk::push_constant]] uniform ConstantBuffer<OffsetPush_Still, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var world = float3(1, 2, 3);
    push.output[0] = world + IVertexOffset_Still_offset(push.materials[0], world);
}
