namespace GenericMethodCheck
{
    struct GenericPush
    {
        int flag;
        float a;
        float3 b;
        float* floats;
        float3* output;
    }

}

using namespace GenericMethodCheck;

float pick_float(bool first, float a, float b)
{
    var chosen = first ? a : b;
    return chosen;
}

float twice_float(bool first, float a, float b)
{
    return pick_float(first, pick_float(first, a, b), b);
}

float load_float(float* buffer, int index)
{
    return buffer[index];
}

float3 pick_float3(bool first, float3 a, float3 b)
{
    var chosen = first ? a : b;
    return chosen;
}

[[vk::push_constant]] uniform ConstantBuffer<GenericPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var first = push.flag != 0;
    var f = twice_float(first, push.a, load_float(push.floats, 1));
    push.output[0] = pick_float3(first, push.b, float3(f));
}
