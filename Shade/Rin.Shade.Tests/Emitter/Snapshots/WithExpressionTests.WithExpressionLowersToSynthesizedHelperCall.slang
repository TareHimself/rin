namespace WithExpressionCheck
{
    struct Bounds3D
    {
        float3 lower;
        float3 upper;
    }

    struct WithPush
    {
        Bounds3D bounds;
        float3 newUpper;
        Bounds3D* output;
    }

}

using namespace WithExpressionCheck;

Bounds3D bounds3DWithUpper(Bounds3D self, float3 upper)
{
    self.upper = upper;
    return self;
}

[[vk::push_constant]] uniform ConstantBuffer<WithPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var grown = bounds3DWithUpper(push.bounds, push.newUpper);
    push.output[0] = grown;
}
