namespace OperatorCheck
{
    struct OperatorPush
    {
        float3 normal;
        int count;
        bool flag;
        float* output;
    }

}

using namespace OperatorCheck;

[[vk::push_constant]] uniform ConstantBuffer<OperatorPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var flipped = -push.normal;
    var notFlag = !push.flag;
    var total = 0;
    total += push.count;
    total -= 1;
    var chosen = push.flag ? 1 : 0;
    push.output[0] = chosen;
}
