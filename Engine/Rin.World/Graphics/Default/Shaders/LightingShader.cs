using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[NoReorder]
public struct LightingInfo
{
    public GBufferHandles GBuffer;
    public Vector3 Eye;
    public BufferRef<LightInfo> Lights;
    public int LightCount;
}

[Shader("Shaders/Rin/World/lighting.slang")]
public partial class LightingShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<LightingInfo> Data;
    }

    public struct VertexIn
    {
        [VertexId] public int VertexId;
    }

    public struct VertexOut
    {
        [Semantic("UV")] public Vector2 Uv;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("UV")] public Vector2 Uv;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    protected override BlendState BlendState => BlendState.Alpha;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var corners = new[]
        {
            new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f),
            new Vector2(-1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f)
        };

        var corner = corners[input.VertexId];
        VertexOut output;
        output.Uv = (corner + new Vector2(1f)) / 2f;
        output.Position = new Vector4(corner.X, corner.Y, 0f, 1f);
        return output;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA16), Stencil]
    public Vector4 Fragment(FragmentIn input)
    {
        var data = Push.Data[0];
        var surface = Bindless.SampleGBuffer(data.GBuffer, input.Uv);

        var color = LightMath.Rgb2Lin(new Vector3(surface.Emissive));
        for (var i = 0; i < data.LightCount; i++) color += DisneyModel.Eval(surface, data.Eye, data.Lights[i]);

        return new Vector4(LightMath.Lin2Rgb(color), 1f);
    }
}
