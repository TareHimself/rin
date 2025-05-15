#include "Engine/Shaders/utils.slang"

struct StencilInfo
{
    // [TextureId,RenderMode,0,0]
    float4x4 transform;
    float2 size;
};

struct PushConstants
{
    float4x4 projection;
    StencilInfo* quads;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;


[shader("vertex")]
float4 vertex(int instanceId: SV_InstanceID, int vertexId: SV_VertexID,out int oQuadIndex: QUAD_INDEX)
    : SV_Position
{
    oQuadIndex = instanceId;
    return float4(vertices[vertexId], 0.0, 1.0);
}

[shader("fragment")]
float4 fragment(in int iQuadIndex: QUAD_INDEX, float2 coordinate: SV_Position)
    : SV_Target
{
    StencilInfo clip = push.quads[iQuadIndex];
    float4 result = applyBorderRadius(coordinate, float4(float3(0.0), 1.0), float4(0.0), clip.size, clip.transform);
    if (result.w > 0.5) {
        discard;
    }
    return float4(float3(1.0), 1.0);
}
