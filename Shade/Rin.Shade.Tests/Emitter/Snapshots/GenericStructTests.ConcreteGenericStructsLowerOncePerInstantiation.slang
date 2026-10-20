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

    struct GenericPush
    {
        Pair_float floats;
        Pair_float3 vectors;
        int flag;
        float3* output;
    }

}

using namespace GenericStructCheck;

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
    push.output[0] = push.vectors.pick(first) * scale.pick(true);
}
