using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader]
public partial class ToneMapShader : Shader
{
    public struct PushConstants
    {
        public DeviceHandle Scene;
        public ToneMapCurve Curve;
    }

    public struct VertexIn
    {
        [Semantic("SV_VulkanVertexID")] public int VertexId;
    }

    public struct VertexOut
    {
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Position] public Vector4 Position;
    }

    [Push] protected PushConstants Push;

    protected override BlendState BlendState => BlendState.Opaque;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        VertexOut output;
        output.Position = new Vector4(ScreenTriangle.Corner(input.VertexId), 0f, 1f);
        return output;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA16)]
    public Vector4 Fragment(FragmentIn input)
    {
        var scene = BindlessData.TexelLoad(Push.Scene, (int)input.Position.X, (int)input.Position.Y).XYZ;
        var display = ToneMapping.Apply(scene, Push.Curve);
        return new Vector4(LightMath.Lin2Rgb(Saturate(display)), 1f);
    }
}
