[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

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

namespace GenericMethodCheck
{
    struct GenericPush
    {
        int flag;
        float a;
        float3 b;
        Rin::Shade::Tests::TestBuffer_float floats;
        Rin::Shade::Tests::TestBuffer_float3 output;
    }

}

using namespace GenericMethodCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

float pick_float(bool first, float a, float b)
{
    var chosen = first ? a : b;
    return chosen;
}

float twice_float(bool first, float a, float b)
{
    return pick_float(first, pick_float(first, a, b), b);
}

float3 pick_float3(bool first, float3 a, float3 b)
{
    var chosen = first ? a : b;
    return chosen;
}

extension Rin::Shade::Tests::TestBuffer_float
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)4;
    }

    float getItem(uint index)
    {
        return rin_testHeap.buffers[(int)this.handle].Load<float>(byteOffset(index));
    }

    float getItem(int index)
    {
        return getItem((uint)index);
    }
}

float load_float(Rin::Shade::Tests::TestBuffer_float buffer, int index)
{
    return buffer.getItem(index);
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

[[vk::push_constant]] uniform ConstantBuffer<GenericPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var first = push.flag != 0;
    var f = twice_float(first, push.a, load_float(push.floats, 1));
    push.output.setItem(0, pick_float3(first, push.b, float3(f)));
}
