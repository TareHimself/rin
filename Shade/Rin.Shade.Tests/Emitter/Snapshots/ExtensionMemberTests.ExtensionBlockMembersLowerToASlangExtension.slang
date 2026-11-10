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

namespace ExtensionBlockCheck
{
    struct ExtensionPush
    {
        float4 value;
        Rin::Shade::Tests::TestBuffer_float output;
    }

}

using namespace ExtensionBlockCheck;

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

extension float4
{
    float sum()
    {
        return this.x + this.y + this.z + this.w;
    }

    float4 scaled(float by)
    {
        return this * by;
    }

    float getMaxComponent()
    {
        return max(max(this.x, this.y), max(this.z, this.w));
    }
}

extension float3
{
    static float3 up()
    {
        return float3(0, 1, 0);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<ExtensionPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var v = push.value;
    push.output.setItem(0, v.scaled(2).sum() + push.value.getMaxComponent() + float3.up().y);
}
