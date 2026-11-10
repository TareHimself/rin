[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
            struct TestBuffer_Gold
            {
                uint handle;
                uint offset;
            }

            struct TestBuffer_Surface
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
    struct SurfacePush_Gold
    {
        Rin::Shade::Tests::TestBuffer_Gold materials;
        Rin::Shade::Tests::TestBuffer_Surface output;
    }

    struct Gold
    {
        float3 tint;
        float roughness;
    }

    struct Surface
    {
        float3 color;
        float roughness;
    }

}

using namespace InterfaceDispatchCheck;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

extension Rin::Shade::Tests::TestBuffer_Gold
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)16;
    }

    Gold getItem(uint index)
    {
        return rin_testHeap.buffers[(int)this.handle].Load<Gold>(byteOffset(index));
    }

    Gold getItem(int index)
    {
        return getItem((uint)index);
    }
}

extension Rin::Shade::Tests::TestBuffer_Surface
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)16;
    }

    void setItem(uint index, Surface value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<Surface>(byteOffset(index), value);
    }

    void setItem(int index, Surface value)
    {
        setItem((uint)index, value);
    }
}

extension InterfaceDispatchCheck::Gold
{
    Surface evaluate(float2 uv)
    {
        Surface surface;
        surface.color = this.tint * uv.x;
        surface.roughness = this.roughness;
        return surface;
    }
}

[[vk::push_constant]] uniform ConstantBuffer<SurfacePush_Gold, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute()
{
    var material = push.materials.getItem(0);
    push.output.setItem(0, material.evaluate(float2(0.5)));
}
