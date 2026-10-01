using System.Numerics;
using Rin.Shade;

namespace Rin.Shade.Tests.Fixtures;

public struct GraphicsPushConstants
{
    public Vector4 Color;
}

public struct VsIn
{
    [Semantic("SV_VertexID")] public int VertexId;
}

public struct VsOut
{
    [Semantic("UV")] public Vector2 Uv;
    [Semantic("SV_Position")] public Vector4 Position;
}

public struct FsIn
{
    [Semantic("UV")] public Vector2 Uv;
}

[Shader("Fixtures/graphics_triangle.slang")]
public class GraphicsTriangleFixtureShader : Shader
{
    [Push] protected GraphicsPushConstants Push;

    [Vertex]
    public virtual VsOut Vertex(VsIn input)
    {
        VsOut output;
        output.Uv = new Vector2(0f, 0f);
        output.Position = new Vector4(0f, 0f, 0f, 1f);
        return output;
    }

    protected override BlendState BlendState => BlendState.Alpha;

    [Fragment, Attachment(AttachmentFormat.RGBA16), Stencil]
    public virtual Vector4 Fragment(FsIn input) => Push.Color;
}
