namespace ExtensionBlockCheck
{
    struct ExtensionPush
    {
        float4 value;
        float* output;
    }

}

using namespace ExtensionBlockCheck;

extension float4
{
    float sum()
    {
        return this.x + this.y + this.z + this.w;
    }

    float4 scaled(float by)
    {
        return this * by;
    }

    float getMaxComponent()
    {
        return max(max(this.x, this.y), max(this.z, this.w));
    }
}

extension float3
{
    static float3 up()
    {
        return float3(0, 1, 0);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<ExtensionPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var v = push.value;
    push.output[0] = v.scaled(2).sum() + push.value.getMaxComponent() + float3.up().y;
}
