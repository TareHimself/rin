using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

[NoReorder]
public struct ImageItem
{
    public Matrix4x4 Transform;
    public Vector2 Size;
    public DeviceHandle Image;
}

[Shader("Shaders/Rin/Core/Views/image_batch.slang")]
public partial class ImageBatchShader : ViewShader<ImageBatchShader.FragmentIn>
{
    public struct PushConstants
    {
        public Matrix4x4 Projection;
        public BufferRef<ImageItem> Images;
    }

    public struct VertexIn
    {
        [InstanceId] public int InstanceId;
        [VertexId] public int VertexId;
    }

    public struct VertexOut
    {
        [Semantic("UV")] public Vector2 Uv;
        [Semantic("INDEX")] public int Index;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("UV")] public Vector2 Uv;
        [Semantic("INDEX")] public int Index;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var item = Push.Images[input.InstanceId];
        var corners = new[]
        {
            new Vector2(0f, 0f), new Vector2(item.Size.X, 0f), new Vector2(item.Size.X, item.Size.Y),
            new Vector2(0f, 0f), new Vector2(item.Size.X, item.Size.Y), new Vector2(0f, item.Size.Y)
        };

        var corner = corners[input.VertexId];
        var projected = Vector4.Transform(Vector4.Transform(new Vector4(corner, 0f, 1f), item.Transform), Push.Projection);

        VertexOut output;
        output.Index = input.InstanceId;
        output.Position = projected with { Z = 0f, W = 1f };
        output.Uv = corner / item.Size;
        return output;
    }

    protected override Vector4 Color(FragmentIn input)
    {
        var item = Push.Images[input.Index];
        return Bindless.SampleTexture(item.Image, input.Uv, ImageTiling.ClampEdge, ImageFilter.Linear);
    }
}
