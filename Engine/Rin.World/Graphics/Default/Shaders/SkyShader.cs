using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader]
public partial class SkyShader : Shader
{
    public struct PushConstants
    {
        public DeviceSpan<WorldInfo> Scene;

        /// <summary>
        ///     The view's forward axis, and its right and up axes scaled to reach the edge of the screen.
        /// </summary>
        public Vector3 Forward;
        public Vector3 Right;
        public Vector3 Up;
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

    [Fragment, Depth, Attachment(AttachmentFormat.RGBA16)]
    public Vector4 Fragment(FragmentIn input)
    {
        // Screen y is -1 on the top row, while the view's up axis points up.
        var direction = Normalize(Push.Forward + Push.Right * input.Screen.X - Push.Up * input.Screen.Y);
        var scene = Push.Scene[0];
        var color = EnvironmentLighting.Backdrop(scene.Environment, direction);
        return new Vector4(color * scene.Exposure, 1f);
    }
}
