using System.Numerics;
using Rin.Shade;

namespace experiment.StencilAndCover.Shaders;

// Pass 2a of stencil-and-cover. Draws the shape's screen-space bounding quad in a solid color, gated by the
// fixed-function stencil test (StencilCoverOp: passes where the winding count from the fill pass is nonzero
// and resets that texel to 0 as it draws). The result is correctly filled but hard-edged.
[Shader("Shaders/StencilAndCover/cover_fill.slang")]
public partial class CoverFillShader : Shader
{
    public struct PushConstants
    {
        public Matrix4x4 Projection;
        public Vector2 MinPos;
        public Vector2 MaxPos;
        public Vector4 Color;
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

    protected override BlendState BlendState => BlendState.Alpha;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var corners = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f),
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
        };

        var position = Push.MinPos + (Push.MaxPos - Push.MinPos) * corners[input.VertexId];

        VertexOut output;
        output.Position = Vector4.Transform(new Vector4(position, 0f, 1f), Push.Projection);
        return output;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA32)]
    public Vector4 Fragment()
    {
        return new Vector4(Push.Color.xyz * Push.Color.W, Push.Color.W);
    }
}
