[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace WithExpressionCheck
{
    struct Bounds3D
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
            struct TestBuffer_Bounds3D
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

namespace WithExpressionCheck
{
    struct WithPush
    {
        Bounds3D bounds;
        float3 newUpper;
        Rin::Shade::Tests::TestBuffer_Bounds3D output;
    }

}

using namespace WithExpressionCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

Bounds3D bounds3DWithUpper(Bounds3D self, float3 upper)
{
    self.upper = upper;
    return self;
}

extension Rin::Shade::Tests::TestBuffer_Bounds3D
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)24;
    }

    void setItem(uint index, Bounds3D value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<Bounds3D>(byteOffset(index), value);
    }

    void setItem(int index, Bounds3D value)
    {
        setItem((uint)index, value);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<WithPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var grown = bounds3DWithUpper(push.bounds, push.newUpper);
    push.output.setItem(0, grown);
}
