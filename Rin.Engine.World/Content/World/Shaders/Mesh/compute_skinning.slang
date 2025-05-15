#include "Engine/Shaders/operators.slang"
#include "Engine/Shaders/utils.slang"
#include "World/Shaders/Mesh/skinned_vertex.slang"
#include "World/Shaders/Mesh/vertex.slang"

struct SkinnedMesh {
    SkinnedVertex *vertices;
}
struct ExecutionInfo {
    int poseId;
    int vertexId;
    int meshId;
}
struct Pose {
    float4x4 *transforms;
}

struct ComputeIn {
    uint3 threadId : SV_DispatchThreadID;
}

struct PushConstants {
    int totalInvocations;
    SkinnedMesh *meshes;
    Pose *poses;
    ExecutionInfo *info;
    Vertex *output;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("compute")]
[numthreads(1, 1, 1)]
void compute(ComputeIn input)
{
    var index = input.threadId.x;
    if (index >= push.totalInvocations) return;
    var info = push.info[index];
    var mesh = push.meshes[info.meshId];
    var inVertex = mesh.vertices[info.vertexId];
    var weights = inVertex.boneWeights;
    var pose = push.poses[info.poseId];
    var boneIds = inVertex.boneIndices;
    float4x4 t1 = pose.transforms[boneIds[0]], t2 = pose.transforms[boneIds[1]], t3 = pose.transforms[boneIds[2]], t4 = pose.transforms[boneIds[3]];
    float4x4 z;
    var location4 = float4(inVertex.getLocation(), 1.0);
    var normal = inVertex.getNormal();
    var location = (t1 * location4 * weights.x) + (t2 * location4 * weights.y) + (t3 * location4 * weights.z) + (t4 * location4 * weights.w);
    normal = (transformNormal(normal, t1) * weights.x) + (transformNormal(normal, t2) * weights.y) + (transformNormal(normal, t3) * weights.z) + (transformNormal(normal, t4) * weights.w);
    normal = normalize(normal);

    Vertex result;
    result._locationU = float4(location.xyz, inVertex.vertex._locationU.w);
    result._normalV = float4(normal, inVertex.vertex._normalV.w);
    result._tangent = inVertex.vertex._tangent;

    push.output[index] = result;
}