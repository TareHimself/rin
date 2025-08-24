#include "Framework/Shaders/operators.slang"
#include "Framework/Shaders/utils.slang"
#include "World/Shaders/Mesh/bounds.slang"
#include "World/Shaders/Mesh/skinned_vertex.slang"
#include "World/Shaders/Mesh/vertex.slang"

enum PathSegmentType : uint {
    Line,
    Bezier,
    Move
}
struct PathSegment {
    PathSegmentType type;
    float2 p0;
    float2 p1;
    float2 p2;
    float2 p3;
}
struct SkinnedMesh {
    int id;
    Vertex *vertices;
    uint count;
}

struct PushConstants {
    SkinnedMesh *skinnedMeshes;
    int totalInvocations;
    Bounds3D *output;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct ComputeIn {
    uint3 threadId : SV_DispatchThreadID;
}

[shader("compute")]
[numthreads(1, 1, 1)]
void compute(ComputeIn input)
{
    var index = input.threadId.x;
    if (index >= push.totalInvocations) return;
    var mesh = push.skinnedMeshes[index];
    var vertex = mesh.vertices[0];
    var vertexCount = mesh.count;
    var bounds = Bounds3D(vertex.getLocation());

    for (var i = 1; i < vertexCount; i++) {
        vertex = mesh.vertices[i];
        bounds.Update(vertex.getLocation());
    }

    push.output[mesh.id] = bounds;
}