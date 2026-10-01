using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

[NoReorder]
public struct BlurData
{
    public Matrix4x4 Transform;
    public Matrix4x4 Projection;
    public DeviceHandle SourceT;
    public Vector2 Size;
    public float Strength;
    public Vector2 Radius;
    public Vector4 Tint;
    public Vector4 DestRect;
}

[Shader("Shaders/Rin/Core/Views/blur.slang")]
public partial class BlurShader : ViewShader<BlurShader.FragmentIn>
{
    public struct PushConstants
    {
        public BufferRef<BlurData> Data;
        public int IsHorizontal;
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
        [Position] public Vector2 Coordinate;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

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

    private static float NormPdf(float x, float sigma)
    {
        return 0.39894f * Math.Exp(-0.5f * x * x / (sigma * sigma)) / sigma;
    }

    protected override Vector4 Color(FragmentIn input)
    {
        var data = Push.Data[0];
        var handle = data.SourceT;
        var imageSize = Bindless.GetTextureSize(handle);

        var p1 = data.DestRect.xy;
        var p2 = data.DestRect.zw;
        var imageUv = (input.Coordinate - p1) / (p2 - p1);
        var texel = new Vector2(1f) / imageSize;

        var horizontal = Push.IsHorizontal == 1;
        var radius = horizontal ? data.Radius.X : data.Radius.Y;
        var kernelRadius = (int)radius;
        var sigma = radius * 0.5f * data.Strength;

        var color = new Vector4(0f);
        var weightSum = 0f;
        for (var i = -kernelRadius; i <= kernelRadius; i++)
        {
            var offset = horizontal ? new Vector2(i * texel.X, 0f) : new Vector2(0f, i * texel.Y);
            var weight = NormPdf(Math.Abs((float)i), sigma);
            var sample = Bindless.SampleTexture(handle, imageUv + offset, ImageTiling.ClampEdge, ImageFilter.Linear);
            color += new Vector4(sample.xyz * sample.W, sample.W) * weight;
            weightSum += weight;
        }

        color /= Math.Max(weightSum, 1e-5f);
        return new Vector4(color.xyz / Math.Max(color.W, 1e-5f), color.W);
    }
}
