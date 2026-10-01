namespace OverrideResolutionCheck
{
    struct ConsumerPush
    {
        float value;
        float* output;
    }

}

using namespace OverrideResolutionCheck;

float getValue(float raw)
{
    return raw * 2;
}

[[vk::push_constant]] uniform ConstantBuffer<ConsumerPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    push.output[0] = getValue(push.value);
}
