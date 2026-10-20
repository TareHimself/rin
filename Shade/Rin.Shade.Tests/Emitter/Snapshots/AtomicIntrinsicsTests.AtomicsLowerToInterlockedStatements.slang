namespace AtomicCheck::AtomicShader
{
    struct AtomicPush
    {
        uint* counters;
        int* signed;
        uint value;
    }

}

using namespace AtomicCheck::AtomicShader;
using namespace AtomicCheck;

[[vk::push_constant]] uniform ConstantBuffer<AtomicPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    InterlockedAdd(push.counters[0], push.value);
    uint previousMin;
    InterlockedMin(push.counters[1], push.value, previousMin);
    InterlockedMax(push.signed[0], (int)push.value);
    InterlockedOr(push.counters[2], 1);
    uint swapped;
    InterlockedExchange(push.counters[3], previousMin, swapped);
    uint seen;
    InterlockedCompareExchange(push.counters[4], swapped, push.value, seen);
    push.counters[5] = seen;
}
