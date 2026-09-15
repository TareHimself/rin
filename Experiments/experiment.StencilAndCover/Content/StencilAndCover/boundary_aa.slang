// boundary_aa.slang — pass 2b of stencil-and-cover.
//
// The interior fill from cover_fill.slang is hard-edged, faceted at the flattened polygon's
// approximation of each curve. This pass draws one thin quad per *original* (unflattened) edge
// and blends an analytic antialiasing correction against the true curve, so the visible boundary
// matches the real Bezier outline rather than its polyline approximation.
//
// Signed-distance-to-quadratic math (dist_to_line/calc_t/dist_to_bezier2) ported from the
// distance formulation already present (but unused/non-functional) in Shaders/Core/Views/path.slang.
// dist_to_line returns a *signed* perpendicular distance — its sign is fixed by the edge's own
// P0->P2 direction, which is why every edge must be fed in its contour's original winding order:
// that's what keeps "inside" a consistent sign across all edges of a shape.

#include "Shaders/Core/Attributes/blending.slang"

float dist_to_line(float2 a, float2 b, float2 p)
{
    float2 dir = b - a;
    float2 norm = float2(-dir.y, dir.x);
    return dot(normalize(norm), a - p);
}

float calc_t(float2 a, float2 b, float2 p)
{
    float2 dir = b - a;
    float t = dot(p - a, dir) / dot(dir, dir);
    return clamp(t, 0.0, 1.0);
}

float dist_to_bezier2(float2 p0, float2 p1, float2 p2, float2 p)
{
    float t = calc_t(p0, p2, p);
    float2 q0 = lerp(p0, p1, t);
    float2 q1 = lerp(p1, p2, t);
    return dist_to_line(q0, q1, p);
}

// Matches EdgeInstance in CoverCommand.cs field-for-field. corner0..3 form an oriented quad
// hugging the curve's chord (not an axis-aligned box), computed on the CPU.
struct EdgeInstance
{
    float2 p0;
    float2 control;
    float2 p2;
    float2 corner0;
    float2 corner1;
    float2 corner2;
    float2 corner3;
    float4 color;
};

struct PushConstants
{
    float4x4 projection;
    EdgeInstance* edges;
};

[[vk::push_constant]]
uniform ConstantBuffer<PushConstants, ScalarDataLayout> push;

struct VSOut
{
    float2 screenPos : SCREEN_POS;
    int instanceId : INSTANCE_ID;
    float4 position : SV_Position;
};

struct FSIn
{
    float2 screenPos : SCREEN_POS;
    int instanceId : INSTANCE_ID;
};

// corner0..3 form a quad as (corner0,corner1,corner2) + (corner1,corner3,corner2).
static const int kCornerIndex[6] = { 0, 1, 2, 1, 3, 2 };

[shader("vertex")]
VSOut vertex(int vid : SV_VertexID, int iid : SV_InstanceID)
{
    EdgeInstance e = push.edges[iid];
    float2 corners[4] = { e.corner0, e.corner1, e.corner2, e.corner3 };
    float2 screenPos = corners[kCornerIndex[vid]];

    VSOut o;
    o.position = mul(push.projection, float4(screenPos, 0.0, 1.0));
    o.screenPos = screenPos;
    o.instanceId = iid;
    return o;
}

[shader("fragment"), Attachment(AttachmentFormat.RGBA32), BlendUI]
float4 fragment(FSIn input) : SV_Target
{
    EdgeInstance e = push.edges[input.instanceId];
    float signedDist = dist_to_bezier2(e.p0, e.control, e.p2, input.screenPos);
    float coverage = saturate(0.5 - signedDist);
    return float4(e.color.rgb * e.color.a * coverage, e.color.a * coverage);
}
