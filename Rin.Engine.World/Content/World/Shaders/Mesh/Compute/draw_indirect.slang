#include "Framework/Shaders/Attributes.slang"
#include "Framework/Shaders/defs.slang"
#include "World/Shaders/Mesh/bounds.slang"

struct IndirectCommand {
    uint indexCount;
    uint instanceCount;
    uint firstIndex;
    int vertexOffset;
    uint firstInstance;
};

struct Mesh {
    uint indicesCount;
    uint indicesStart;
    uint vertexStart;
    uint instance; // The material instance
    int meshIndex;
    IndirectCommand toIndirectCommand() {
        return IndirectCommand(indicesCount, 1, indicesStart, vertexStart, instance);
    }
};

struct Frustum {
    // left,right,bottom,top,near,far
    float4 planes[6];
}

struct PushConstants {
    // Frustum *frustums;
    // uint viewCount;
    uint *cullingResults;
    Mesh *meshes;
    uint invocationCount;
    IndirectCommand *output;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[[RIN_BINDING(0,0)]]
[StorageBufferBinding]
uniform RWByteAddressBuffer drawCount;

struct ComputeIn {
    uint3 threadId : SV_DispatchThreadID;
}

bool isInView(Bounds3D bounds, Frustum frustum) {
    // float3 corners[8] = {
    //     float3(bounds.x.min, bounds.y.min, bounds.z.min),
    //     float3(bounds.x.min, bounds.y.min, bounds.z.max),
    //     float3(bounds.x.min, bounds.y.max, bounds.z.min),
    //     float3(bounds.x.min, bounds.y.max, bounds.z.max),
    //     float3(bounds.x.max, bounds.y.min, bounds.z.min),
    //     float3(bounds.x.max, bounds.y.min, bounds.z.max),
    //     float3(bounds.x.max, bounds.y.max, bounds.z.min),
    //     float3(bounds.x.max, bounds.y.max, bounds.z.max)
    // };

    // for (int i = 0; i < 6; ++i) {
    //     float4 plane = frustum.planes[i];
    //     bool allOutside = true;
    //     for (int j = 0; j < 8; ++j) {
    //         float3 corner = corners[j];
    //         float distance = dot(plane.xyz, corner) + plane.w;
    //         if (distance >= 0.0f) {
    //             allOutside = false;
    //             break;
    //         }
    //     }
    //     if (allOutside)
    //         return false;
    // }
    return true;
}

[shader("compute")]
[numthreads(1, 1, 1)]
void compute(ComputeIn input)
{
    var index = input.threadId.x;
    if (index >= push.invocationCount) return;

    var mesh = push.meshes[index];
    
    // Do culling etc here
    var wasCulled = push.cullingResults[mesh.meshIndex] == 0;

    if (wasCulled) return;

    uint drawIndex = 0;
    drawCount.InterlockedAdd(0, 1, drawIndex);

    push.output[drawIndex] = mesh.toIndirectCommand();
}