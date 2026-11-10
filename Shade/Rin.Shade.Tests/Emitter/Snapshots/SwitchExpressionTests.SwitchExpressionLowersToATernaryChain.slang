[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace SwitchExpressionCheck
{
    enum Shape
    {
        Circle = 0,
        Square = 1,
        Diamond = 2
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

namespace SwitchExpressionCheck
{
    struct SwitchPush
    {
        Shape shape;
        float distance;
        int lane;
        float4 value;
        Rin::Shade::Tests::TestBuffer_float output;
    }

}

using namespace SwitchExpressionCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

float lane(float4 value, int index)
{
    return index == 0 ? value.x : index == 1 ? value.y : index == 2 ? value.z : value.w;
}

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

[[vk::push_constant]] uniform ConstantBuffer<SwitchPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var radius = push.shape == Shape.Circle ? 1.0 : push.shape == Shape.Square ? 1.5 : 2.0;
    float switchValue0 = push.distance * 2;
    var band = switchValue0 < 0.25 ? 0.0 : (switchValue0 >= 0.25 && switchValue0 < 0.75) ? 0.5 : (switchValue0 > 0.9 || switchValue0 < -0.9) && push.lane > 0 ? 0.75 : 1.0;
    push.output.setItem(0, radius + band + lane(push.value, push.lane));
}
