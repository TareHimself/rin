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

namespace BindingGroupCheck
{
    struct BindingPush
    {
        int index;
        Rin::Shade::Tests::TestBuffer_float output;
    }

    struct MaterialResources
    {
        SamplerState samplers[4];
        Texture2D readTextures[8];
    }

}

using namespace BindingGroupCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;
ParameterBlock<MaterialResources> material;

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

[[vk::push_constant]] uniform ConstantBuffer<BindingPush, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var t = material.readTextures[push.index];
    push.output.setItem(0, 1);
}
