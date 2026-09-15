// cover_fill.slang — pass 2a of stencil-and-cover.
//
// Draws the shape's screen-space bounding quad with a solid fill color, gated by the
// fixed-function stencil test (StencilCoverOp: passes where the winding count accumulated by
// stencil_fill.slang is nonzero, and resets that texel back to 0 as it draws). This alone gives
// a correctly filled but hard-edged shape — boundary_aa.slang draws over it to fix the edges.

#include "Shaders/Core/Attributes/blending.slang"

struct PushConstants
{
    float4x4 projection;
    float2 minPos;
    float2 maxPos;
    float4 color;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

static const int2 kCorners[6] = {
    int2(0, 0), int2(1, 0), int2(0, 1),
    int2(1, 0), int2(1, 1), int2(0, 1)
};

[shader("vertex")]
float4 vertex(int vid : SV_VertexID) : SV_Position
{
    float2 corner = float2(kCorners[vid]);
    float2 pos = lerp(push.minPos, push.maxPos, corner);
    return mul(push.projection, float4(pos, 0.0, 1.0));
}

[shader("fragment"), Attachment(AttachmentFormat.RGBA32), BlendUI]
float4 fragment() : SV_Target
{
    return float4(push.color.rgb * push.color.a, push.color.a);
}
