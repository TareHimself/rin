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

            struct TestHeap
            {
                RWByteAddressBuffer buffers[8];
            }

        }

    }

}

namespace MathIntrinsicsCheck
{
    struct MathPush
    {
        float3 a;
        float3 b;
        Rin::Shade::Tests::TestBuffer_float output;
    }

}

using namespace MathIntrinsicsCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

extension Rin::Shade::Tests::TestBuffer_float
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)4;
    }

    void setItem(uint index, float value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<float>(byteOffset(index), value);
    }

    void setItem(int index, float value)
    {
        setItem((uint)index, value);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<MathPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var n = normalize(push.a);
    var d = dot(n, push.b);
    var c = clamp(d, 0, 1);
    push.output.setItem(0, c);
}
