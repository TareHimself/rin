namespace Rin::Shade::Tests::Fixtures
{
    struct Vertex
    {
        float4 locationU;
        float4 normalV;
        float4 tangent;
    }

    struct SkinnedMesh
    {
        int index;
        Vertex* vertices;
        uint count;
    }

    struct UpdatableBounds3D
    {
        float3 lower;
        float3 upper;
    }

    struct BoundsUpdatePushConstants
    {
        SkinnedMesh* skinnedMeshes;
        int totalInvocations;
        UpdatableBounds3D* output;
    }

    struct ComputeIn
    {
        uint threadId : SV_DispatchThreadID;
    }

}

using namespace Rin::Shade::Tests::Fixtures;

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
    var mesh = push.skinnedMeshes[index];
    var vertex = mesh.vertices[0];
    var vertexCount = mesh.count;
    var bounds = UpdatableBounds3D(vertex.getLocation());
    for (var i = 1; i < vertexCount; i++)
    {
        vertex = mesh.vertices[i];
        bounds.update(vertex.getLocation());
    }
    push.output[mesh.index] = bounds;
}
