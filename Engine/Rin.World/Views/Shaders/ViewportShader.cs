using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Rin.World.Views.Shaders;

[NoReorder]
public struct ViewportPushData
{
    public Matrix4x4 Projection;
    public Matrix4x4 Transform;
    public Vector2 Size;
    public DeviceHandle OutputImage;
}

[Shader]
public partial class ViewportShader : ViewShader<ViewportShader.FragmentIn>
{
    public struct PushConstants
    {
        public DeviceSpan<ViewportPushData> Data;
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

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var data = Push.Data[0];
        var corners = new[]
        {
            new Vector2(0f, 0f), new Vector2(data.Size.X, 0f), new Vector2(data.Size.X, data.Size.Y),
            new Vector2(0f, 0f), new Vector2(data.Size.X, data.Size.Y), new Vector2(0f, data.Size.Y)
        };

        var corner = corners[input.VertexId];
        var projected = Vector4.Transform(Vector4.Transform(new Vector4(corner, 0f, 1f), data.Transform), data.Projection);

        VertexOut output;
        output.Position = new Vector4(projected.X, projected.Y, 0f, 1f);
        output.Uv = corner / data.Size;
        return output;
    }

    protected override Vector4 Color(FragmentIn input)
    {
        var data = Push.Data[0];
        return BindlessData.SampleTexture(data.OutputImage, input.Uv, ImageTiling.Repeat, ImageFilter.Linear);
    }
}
