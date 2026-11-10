namespace AtomicCheck
{
    namespace AtomicShader
    {
        struct AtomicPush
        {
            uint value;
        }

    }

    struct AtomicBindings
    {
        RWByteAddressBuffer counters;
    }

}

using namespace AtomicCheck::AtomicShader;
using namespace AtomicCheck;

ParameterBlock<AtomicBindings> bindings;

groupshared uint shared;
groupshared int signed;

[[vk::push_constant]] uniform ConstantBuffer<AtomicPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    InterlockedAdd(shared, push.value);
    uint previousMin;
    InterlockedMin(shared, push.value, previousMin);
    InterlockedMax(signed, (int)push.value);
    bindings.counters.InterlockedOr(8u, 1u);
    uint swapped;
    bindings.counters.InterlockedExchange(12u, previousMin, swapped);
    uint seen;
    bindings.counters.InterlockedCompareExchange(16u, swapped, push.value, seen);
    bindings.counters.Store<uint>(20u, seen);
}
