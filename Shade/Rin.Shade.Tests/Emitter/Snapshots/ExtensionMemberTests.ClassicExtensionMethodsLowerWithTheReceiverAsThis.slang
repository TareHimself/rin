[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace ClassicExtensionCheck
{
    struct Bounds
    {
        float3 lower;
        float3 upper;
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

namespace ClassicExtensionCheck
{
    struct BoundsPush
    {
        Bounds bounds;
        Rin::Shade::Tests::TestBuffer_float3 output;
    }

}

using namespace ClassicExtensionCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

extension ClassicExtensionCheck::Bounds
{
    [mutating]
    void grow(float by)
    {
        this.lower -= float3(by);
        this.upper += float3(by);
    }

    Bounds grown(float by)
    {
        Bounds b = this;
        b.lower -= float3(by);
        b.upper += float3(by);
        return b;
    }

    float3 size()
    {
        return this.upper - this.lower;
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

[[vk::push_constant]] uniform ConstantBuffer<BoundsPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var bounds = push.bounds;
    bounds.grow(1);
    var grown = bounds.grown(2);
    push.output.setItem(0, grown.size() + bounds.size());
}
