using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader]
public partial class EnvironmentFilterShader : Shader
{
    public struct PushConstants
    {
        public DeviceHandle Panorama;
        public int Face;
        public float Roughness;
        public float SharpestLevel;
        public Vector2 PanoramaSize;
    }

    public struct VertexIn
    {
        [Semantic("SV_VulkanVertexID")] public int VertexId;
    }

    public struct VertexOut
    {
        [Position] public Vector4 Position;
        [Semantic("SCREEN")] public Vector2 Screen;
    }

    public struct FragmentIn
    {
        [Position] public Vector4 Position;
        [Semantic("SCREEN")] public Vector2 Screen;
    }

    [Push] protected PushConstants Push;

    protected override BlendState BlendState => BlendState.Opaque;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        VertexOut output;
        output.Screen = ScreenTriangle.Corner(input.VertexId);
        output.Position = new Vector4(output.Screen, 0f, 1f);
        return output;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA16)]
    public Vector4 Fragment(FragmentIn input)
    {
        // The top row of the target is screen y -1, which is also the top of a cubemap face.
        var normal = EnvironmentFiltering.FaceDirection(Push.Face, input.Screen.X, input.Screen.Y);
        var color = EnvironmentFiltering.Blur(Push.Panorama, normal, Push.Roughness, Push.SharpestLevel,
            Push.PanoramaSize);
        return new Vector4(color, 1f);
    }
}
