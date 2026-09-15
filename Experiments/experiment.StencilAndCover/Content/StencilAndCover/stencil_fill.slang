// stencil_fill.slang — pass 1 of stencil-and-cover.
//
// Draws each contour's triangle fan into a dedicated stencil attachment (bound by the caller
// with StencilFillOp: front-face INCREMENT_AND_WRAP, back-face DECREMENT_AND_WRAP). No color is
// written — a shape's true interior/exterior classification (including holes from
// oppositely-wound contours, and self-intersections) falls out of the accumulated winding count
// entirely on the GPU, not from any CPU-side polygon logic.

struct PushConstants
{
    float4x4 projection;
    float4x4 transform;
    float2* vertices;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

[shader("vertex")]
float4 vertex(int vid : SV_VertexID) : SV_Position
{
    float2 local = push.vertices[vid];
    return mul(push.projection, mul(push.transform, float4(local, 0.0, 1.0)));
}

[shader("fragment"), BlendNone, Stencil]
void fragment()
{
}
