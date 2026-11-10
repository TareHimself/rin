[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace GenericStructCheck
{
    struct Pair_float
    {
        float first;
        float second;
    }

    struct Pair_float3
    {
        float3 first;
        float3 second;
    }

}

namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
            struct TestBuffer_float3
            {
                uint handle;
                uint offset;
            }

            struct TestHeap
            {
                RWByteAddressBuffer buffers[8];
            }

        }

    }

}

namespace GenericStructCheck
{
    struct GenericPush
    {
        Pair_float floats;
        Pair_float3 vectors;
        int flag;
        Rin::Shade::Tests::TestBuffer_float3 output;
    }

}

using namespace GenericStructCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

extension GenericStructCheck::Pair_float
{
    static Pair_float of(float first, float second)
    {
        Pair_float pair;
        pair.first = first;
        pair.second = second;
        return pair;
    }

    float pick(bool first)
    {
        return first ? this.first : this.second;
    }
}

extension Rin::Shade::Tests::TestBuffer_float3
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)12;
    }

    void setItem(uint index, float3 value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<float3>(byteOffset(index), value);
    }

    void setItem(int index, float3 value)
    {
        setItem((uint)index, value);
    }
}

extension GenericStructCheck::Pair_float3
{
    float3 pick(bool first)
    {
        return first ? this.first : this.second;
    }
}

[[vk::push_constant]] uniform ConstantBuffer<GenericPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var first = push.flag != 0;
    var scale = Pair_float.of(push.floats.pick(first), 2);
    push.output.setItem(0, push.vectors.pick(first) * scale.pick(true));
}
