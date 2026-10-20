namespace ClassicExtensionCheck
{
    struct Bounds
    {
        float3 lower;
        float3 upper;
    }

    struct BoundsPush
    {
        Bounds bounds;
        float3* output;
    }

}

using namespace ClassicExtensionCheck;

extension ClassicExtensionCheck::Bounds
{
    [mutating]
    void grow(float by)
    {
        this.lower -= float3(by);
        this.upper += float3(by);
    }

    Bounds grown(float by)
    {
        Bounds b = this;
        b.lower -= float3(by);
        b.upper += float3(by);
        return b;
    }

    float3 size()
    {
        return this.upper - this.lower;
    }
}

[[vk::push_constant]] uniform ConstantBuffer<BoundsPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var bounds = push.bounds;
    bounds.grow(1);
    var grown = bounds.grown(2);
    push.output[0] = grown.size() + bounds.size();
}
