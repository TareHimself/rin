namespace BindingGroupCheck
{
    struct BindingPush
    {
        int index;
        float* output;
    }

    struct MaterialResources
    {
        SamplerState samplers[4];
        Texture2D readTextures[8];
    }

}

using namespace BindingGroupCheck;

ParameterBlock<MaterialResources> material;

[[vk::push_constant]] uniform ConstantBuffer<BindingPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var t = material.readTextures[push.index];
    push.output[0] = 1;
}
