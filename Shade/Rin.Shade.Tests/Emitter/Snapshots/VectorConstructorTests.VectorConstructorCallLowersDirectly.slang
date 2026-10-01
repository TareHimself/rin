namespace VectorConstructorCheck
{
    struct VectorConstructorPush
    {
        float3* output;
    }

}

using namespace VectorConstructorCheck;

[[vk::push_constant]] uniform ConstantBuffer<VectorConstructorPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var v = float3(1, 2, 3);
    push.output[0] = v;
}
