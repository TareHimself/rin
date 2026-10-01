using System.Numerics;
using Rin.Shade;

namespace experiment.StencilAndCover.Shaders;

// Pass 1 of stencil-and-cover. Draws each contour's triangle fan into a dedicated stencil attachment, bound
// by the caller with StencilFillOp (front-face increment and wrap, back-face decrement and wrap). No color is
// written: a shape's interior, holes from oppositely wound contours and self-intersections all fall out of
// the accumulated winding count on the GPU.
[Shader("Shaders/StencilAndCover/stencil_fill.slang")]
public partial class StencilFillShader : Shader
{
    public struct PushConstants
    {
        public Matrix4x4 Projection;
        public Matrix4x4 Transform;
        public BufferRef<Vector2> Vertices;
    }

    public struct VertexIn
    {
        [VertexId] public int VertexId;
    }

    public struct VertexOut
    {
        [Position] public Vector4 Position;
    }

    [Push] protected PushConstants Push;

    protected override BlendState BlendState => BlendState.None;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var local = Push.Vertices[input.VertexId];
        var world = Vector4.Transform(new Vector4(local, 0f, 1f), Push.Transform);

        VertexOut output;
        output.Position = Vector4.Transform(world, Push.Projection);
        return output;
    }

    [Fragment, Stencil]
    public void Fragment()
    {
    }
}
