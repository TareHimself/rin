using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

/// <summary>
///     One video quad: its luma plane (R8) and interleaved chroma plane (RG8) as decoded, converted to RGB here
///     instead of on the CPU. <see cref="Colorimetry" /> picks the matrix, since the container does not say.
/// </summary>
[NoReorder]
public struct VideoItem
{
    public Matrix4x4 Transform;
    public Vector2 Size;
    public DeviceHandle Luma;
    public DeviceHandle Chroma;
    public VideoColorimetry Colorimetry;
}

/// <summary>
///     The YCbCr to RGB matrix of a video, both limited range. HD video is almost always BT.709 and SD BT.601.
/// </summary>
public enum VideoColorimetry
{
    Bt601,
    Bt709
}

[Shader]
public partial class VideoBatchShader : ViewShader<VideoBatchShader.FragmentIn>
{
    public struct PushConstants
    {
        public Matrix4x4 Projection;
        public BufferRef<VideoItem> Items;
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
        var item = Push.Items[input.InstanceId];
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
        var item = Push.Items[input.Index];
        var luma = Bindless.SampleTexture(item.Luma, input.Uv, ImageTiling.ClampEdge, ImageFilter.Linear).X;
        var chroma = Bindless.SampleTexture(item.Chroma, input.Uv, ImageTiling.ClampEdge, ImageFilter.Linear).XY;

        // Limited range: luma spans 16..235 and chroma 16..240 of 255, centred on 128.
        var y = (luma - 16f / 255f) * (255f / 219f);
        var cb = (chroma.X - 128f / 255f) * (255f / 224f);
        var cr = (chroma.Y - 128f / 255f) * (255f / 224f);

        var rgb = item.Colorimetry == VideoColorimetry.Bt709
            ? new Vector3(y + 1.5748f * cr, y - 0.1873f * cb - 0.4681f * cr, y + 1.8556f * cb)
            : new Vector3(y + 1.402f * cr, y - 0.3441f * cb - 0.7141f * cr, y + 1.772f * cb);

        return new Vector4(Saturate(rgb), 1f);
    }
}
