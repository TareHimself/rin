using System.Numerics;
using experiment.StencilAndCover.Rendering;
using Rin.Shade;

namespace experiment.StencilAndCover.Shaders;

// Pass 2b of stencil-and-cover. The interior fill is faceted at the flattened polygon's approximation of each
// curve, so this draws one thin quad per original edge and blends an analytic antialiasing correction against
// the true curve. The signed distance is fixed by the edge's own P0 to P2 direction, which is why every edge
// must be fed in its contour's original winding order.
[Shader("Shaders/StencilAndCover/boundary_aa.slang")]
public partial class BoundaryAaShader : Shader
{
    public struct PushConstants
    {
        public Matrix4x4 Projection;
        public BufferRef<EdgeInstance> Edges;
    }

    public struct VertexIn
    {
        [VertexId] public int VertexId;
        [InstanceId] public int InstanceId;
    }

    public struct VertexOut
    {
        [Semantic("SCREEN_POS")] public Vector2 ScreenPos;
        [Semantic("INSTANCE_ID")] public int InstanceId;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("SCREEN_POS")] public Vector2 ScreenPos;
        [Semantic("INSTANCE_ID")] public int InstanceId;
    }

    [Push] protected PushConstants Push;

    protected override BlendState BlendState => BlendState.Alpha;

    private static float DistToLine(Vector2 a, Vector2 b, Vector2 p)
    {
        var direction = b - a;
        var normal = new Vector2(-direction.Y, direction.X);
        return Math.Dot(Math.Normalize(normal), a - p);
    }

    private static float CalcT(Vector2 a, Vector2 b, Vector2 p)
    {
        var direction = b - a;
        var t = Math.Dot(p - a, direction) / Math.Dot(direction, direction);
        return Math.Clamp(t, 0f, 1f);
    }

    private static float DistToBezier2(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p)
    {
        var t = CalcT(p0, p2, p);
        var q0 = Math.Lerp(p0, p1, t);
        var q1 = Math.Lerp(p1, p2, t);
        return DistToLine(q0, q1, p);
    }

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var edge = Push.Edges[input.InstanceId];
        var corners = new[] { edge.Corner0, edge.Corner1, edge.Corner2, edge.Corner3 };
        var cornerIndex = new[] { 0, 1, 2, 1, 3, 2 };
        var screenPos = corners[cornerIndex[input.VertexId]];

        VertexOut output;
        output.Position = Vector4.Transform(new Vector4(screenPos, 0f, 1f), Push.Projection);
        output.ScreenPos = screenPos;
        output.InstanceId = input.InstanceId;
        return output;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA32)]
    public Vector4 Fragment(FragmentIn input)
    {
        var edge = Push.Edges[input.InstanceId];
        var signedDistance = DistToBezier2(edge.P0, edge.Control, edge.P2, input.ScreenPos);
        var coverage = Math.Saturate(0.5f - signedDistance);
        return new Vector4(edge.Color.xyz * edge.Color.W * coverage, edge.Color.W * coverage);
    }
}
