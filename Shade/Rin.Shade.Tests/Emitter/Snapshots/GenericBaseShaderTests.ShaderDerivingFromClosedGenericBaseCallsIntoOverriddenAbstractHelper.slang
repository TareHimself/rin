namespace GenericBaseCheck
{
    struct ConsumerPush
    {
        float value;
        float* output;
    }

}

using namespace GenericBaseCheck;

float getValue(ConsumerPush push)
{
    return push.value;
}

[[vk::push_constant]] uniform ConstantBuffer<ConsumerPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var v = getValue(push);
}
