#include "Framework/Shaders/Attributes.slang"
#include "Engine/Shaders/defs.slang"
#include "World/Shaders/Mesh/bounds.slang"

struct Frustum {
    // left,right,bottom,top,near,far
    float4 planes[6];
}

struct PushConstants {
    // Frustum *frustums;
    // uint viewCount;
    Bounds3D *bounds;
    uint invocationCount;
    uint *output;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

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

    var bounds = push.bounds[index];

    push.output[index] = 1;
    // Do culling etc here

    // uint drawIndex = 0;
    // drawCount.InterlockedAdd(0, 1, drawIndex);

    //push.output[drawIndex] = mesh.toIndirectCommand();
}