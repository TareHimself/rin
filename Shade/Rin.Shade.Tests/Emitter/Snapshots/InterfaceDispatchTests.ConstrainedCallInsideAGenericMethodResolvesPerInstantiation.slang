[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

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

}

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

namespace InterfaceDispatchCheck
{
    struct ScalePush
    {
        Half a;
        Double b;
        Rin::Shade::Tests::TestBuffer_float output;
    }

}

using namespace InterfaceDispatchCheck;

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
    push.output.setItem(0, apply_Half(push.a) + apply_Double(push.b));
}
