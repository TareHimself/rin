namespace MathIntrinsicsCheck
{
    struct MathPush
    {
        float3 a;
        float3 b;
        float* output;
    }

}

using namespace MathIntrinsicsCheck;

[[vk::push_constant]] uniform ConstantBuffer<MathPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var n = normalize(push.a);
    var d = dot(n, push.b);
    var c = clamp(d, 0, 1);
    push.output[0] = c;
}
