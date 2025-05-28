#include "Engine/Shaders/attributes.slang"
#include "Engine/Shaders/defs.slang"

struct Bounds1D {
    float min;
    float max;
};
struct Bounds3D {
    Bounds1D x;
    Bounds1D y;
    Bounds1D z;
}

struct IndirectCommand {
    uint indexCount;
    uint instanceCount;
    uint firstIndex;
    int vertexOffset;
    uint firstInstance;
};

struct Mesh {
    Bounds3D bounds;
    uint indicesCount;
    uint indicesStart;
    uint vertexStart;
    uint instance; // The material instance
    uint meshId;
    IndirectCommand toIndirectCommand() {
        return IndirectCommand(indicesCount, 1, indicesStart, vertexStart, instance);
    }
};

struct PushConstants {
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

[shader("compute")]
[numthreads(1, 1, 1)]
void compute(ComputeIn input)
{
    var index = input.threadId.x;
    if (index >= push.invocationCount) return;

    var mesh = push.meshes[index];

    // Do culling etc here

    uint drawIndex = 0;
    drawCount.InterlockedAdd(0, 1, drawIndex);

    push.output[drawIndex] = mesh.toIndirectCommand();
}