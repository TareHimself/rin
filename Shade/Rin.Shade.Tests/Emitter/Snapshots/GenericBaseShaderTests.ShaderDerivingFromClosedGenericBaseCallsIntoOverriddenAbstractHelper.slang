namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
            struct TestBuffer_float
            {
                uint handle;
                uint offset;
            }

        }

    }

}

namespace GenericBaseCheck
{
    struct ConsumerPush
    {
        float value;
        Rin::Shade::Tests::TestBuffer_float output;
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
