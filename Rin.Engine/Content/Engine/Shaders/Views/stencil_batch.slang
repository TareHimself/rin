#include "Engine/Shaders/Attributes/blending.slang"
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
    StencilInfo* clips;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

const static float2 _stencil_internal_vertices[] = { float2(-1.0), float2(1.0, -1.0), float2(1.0), float2(-1.0), float2(1.0), float2(-1.0, 1.0) };
[shader("vertex")]
float4 vertex(int instanceId: SV_InstanceID, int vertexId: SV_VertexID,out int oQuadIndex: QUAD_INDEX)
    : SV_Position
{
    oQuadIndex = instanceId;
    return float4(_stencil_internal_vertices[vertexId], 0.0, 1.0);
}

// // Seems like we have to output something so BlendNone combined with an attachment will set write mask to none
// [shader("fragment"), Attachment(AttachmentFormat.RGBA32),BlendNone, Stencil]
// float4 fragment(in int iQuadIndex: QUAD_INDEX, float2 coordinate: SV_Position) : SV_Target
// {
//     StencilInfo clip = push.clips[iQuadIndex];
//     float4 result = applyBorderRadius(coordinate, float4(float3(0.0), 1.0), float4(0.0), clip.size, clip.transform);
//     if (result.w > 0.01) {
//         discard;
//     }

//     return float4(1.0);
// }
// Seems like we have to output something so BlendNone combined with an attachment will set write mask to none
[shader("fragment"), BlendNone, Stencil]
void fragment(in int iQuadIndex: QUAD_INDEX, float2 coordinate: SV_Position)
{
    StencilInfo clip = push.clips[iQuadIndex];
    float4 result = applyBorderRadius(coordinate, float4(float3(0.0), 1.0), float4(0.0), clip.size, clip.transform);
    if (result.w > 0.01) {
        discard;
    }
}
