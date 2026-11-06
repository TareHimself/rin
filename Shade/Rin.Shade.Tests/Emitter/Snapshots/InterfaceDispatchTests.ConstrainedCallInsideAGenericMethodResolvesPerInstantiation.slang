namespace InterfaceDispatchCheck
{
    struct Half
    {
        float value;
    }

    struct Double
    {
        float value;
    }

    struct ScalePush
    {
        Half a;
        Double b;
        float* output;
    }

}

using namespace InterfaceDispatchCheck;

extension InterfaceDispatchCheck::Half
{
    static float scale(in Half value)
    {
        return value.value * 0.5;
    }
}

float apply_Half(in Half value)
{
    return Half.scale(value);
}

extension InterfaceDispatchCheck::Double
{
    static float scale(in Double value)
    {
        return value.value * 2;
    }
}

float apply_Double(in Double value)
{
    return Double.scale(value);
}

[[vk::push_constant]] uniform ConstantBuffer<ScalePush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    push.output[0] = apply_Half(push.a) + apply_Double(push.b);
}
