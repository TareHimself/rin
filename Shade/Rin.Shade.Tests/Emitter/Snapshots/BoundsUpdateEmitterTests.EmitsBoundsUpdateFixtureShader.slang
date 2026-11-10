[__AttributeUsage(_AttributeTargets.Var)]
struct BindlessBlockAttribute { string name; };

namespace Rin
{
    namespace Shade
    {
        namespace Tests
        {
            struct TestBuffer_SkinnedMesh
            {
                uint handle;
                uint offset;
            }

            struct TestBuffer_UpdatableBounds3D
            {
                uint handle;
                uint offset;
            }

            struct TestBuffer_Vertex
            {
                uint handle;
                uint offset;
            }

            struct TestHeap
            {
                RWByteAddressBuffer buffers[8];
            }

            namespace Fixtures
            {
                struct BoundsUpdatePushConstants
                {
                    TestBuffer_SkinnedMesh skinnedMeshes;
                    int totalInvocations;
                    TestBuffer_UpdatableBounds3D output;
                }

                struct ComputeIn
                {
                    uint threadId : SV_DispatchThreadID;
                }

                struct SkinnedMesh
                {
                    int index;
                    TestBuffer_Vertex vertices;
                    uint count;
                }

                struct Vertex
                {
                    float4 locationU;
                    float4 normalV;
                    float4 tangent;
                }

                struct UpdatableBounds3D
                {
                    float3 lower;
                    float3 upper;
                }

            }

        }

    }

}

using namespace Rin::Shade::Tests::Fixtures;

[BindlessBlock("test.heap")] ParameterBlock<Rin::Shade::Tests::TestHeap> rin_testHeap;

extension Rin::Shade::Tests::TestBuffer_SkinnedMesh
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)16;
    }

    SkinnedMesh getItem(uint index)
    {
        return rin_testHeap.buffers[(int)this.handle].Load<SkinnedMesh>(byteOffset(index));
    }
}

extension Rin::Shade::Tests::TestBuffer_Vertex
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)48;
    }

    Vertex getItem(uint index)
    {
        return rin_testHeap.buffers[(int)this.handle].Load<Vertex>(byteOffset(index));
    }

    Vertex getItem(int index)
    {
        return getItem((uint)index);
    }
}

extension Rin::Shade::Tests::Fixtures::UpdatableBounds3D
{
    __init(float3 location)
    {
        this.lower = location;
        this.upper = location;
    }

    [mutating]
    void update(float3 location)
    {
        this.lower = min(this.lower, location);
        this.upper = max(this.upper, location);
    }
}

extension Rin::Shade::Tests::Fixtures::Vertex
{
    float3 getLocation()
    {
        return this.locationU.xyz;
    }
}

extension Rin::Shade::Tests::TestBuffer_UpdatableBounds3D
{
    uint byteOffset(uint index)
    {
        return this.offset + index * (uint)24;
    }

    void setItem(uint index, UpdatableBounds3D value)
    {
        rin_testHeap.buffers[(int)this.handle].Store<UpdatableBounds3D>(byteOffset(index), value);
    }

    void setItem(int index, UpdatableBounds3D value)
    {
        setItem((uint)index, value);
    }
}

[[vk::push_constant]] uniform ConstantBuffer<BoundsUpdatePushConstants, ScalarDataLayout> push;

[shader("compute")]
[numthreads(64, 1, 1)]
void compute(ComputeIn input)
{
    var index = input.threadId;
    if (index >= (uint)push.totalInvocations)
    {
        return;
    }
    var mesh = push.skinnedMeshes.getItem(index);
    var vertex = mesh.vertices.getItem(0);
    var vertexCount = mesh.count;
    var bounds = UpdatableBounds3D(vertex.getLocation());
    for (var i = 1u; i < vertexCount; i++)
    {
        vertex = mesh.vertices.getItem(i);
        bounds.update(vertex.getLocation());
    }
    push.output.setItem(mesh.index, bounds);
}
