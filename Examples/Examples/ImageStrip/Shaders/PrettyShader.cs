using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Views;
using Rin.Core.Views.Graphics.Shaders;
using Rin.Shade;

namespace Examples.ImageStrip.Shaders;

[NoReorder]
public struct PrettyData
{
    public Matrix4x4 Projection;
    public Matrix4x4 Transform;
    public BorderRadius BorderRadius;
    public Vector2 Size;
    public Vector2 Focus;
    public float Time;
}

[Shader]
public partial class PrettyShader : ViewShader<PrettyShader.FragmentIn>
{
    public struct PushConstants
    {
        public BufferRef<PrettyData> Data;
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

    // https://iquilezles.org/articles/palettes/
    private static Vector3 Palette(float t)
    {
        var a = new Vector3(0.5f);
        var b = new Vector3(0.5f);
        var c = new Vector3(1f);
        var d = new Vector3(0.263f, 0.416f, 0.557f);
        return a + b * Cos(6.28318f * (c * t + d));
    }

    // https://www.shadertoy.com/view/fsdyzB
    private static float SdRoundedBox(Vector2 p, Vector2 halfSize, BorderRadius radius)
    {
        var cornerRadius = p.X > 0f
            ? p.Y > 0f ? radius.BottomRight : radius.TopRight
            : p.Y > 0f ? radius.BottomLeft : radius.TopLeft;

        var q = Abs(p) - halfSize + new Vector2(cornerRadius);
        return Min(Max(q.X, q.Y), 0f) + Length(Max(q, new Vector2(0f))) - cornerRadius;
    }

    // https://www.shadertoy.com/view/mtyGWy
    protected override Vector4 Color(FragmentIn input)
    {
        var data = Push.Data[0];
        var local = input.Uv * data.Size;
        var uv = (local - data.Focus) * 2f / data.Size.Y;
        var uv0 = uv;

        var finalColor = new Vector3(0f);
        for (var i = 0f; i < 4f; i++)
        {
            uv = Frac(uv * 1.5f) - new Vector2(0.5f);

            var d = Length(uv) * Exp(Length(uv0) * -1f);
            var color = Palette(Length(uv0) + i * 0.4f + data.Time * 0.4f);

            d = Sin(d * 8f + data.Time) / 8f;
            d = Abs(d);
            d = Pow(0.01f / d, 1.2f);

            finalColor += color * d;
        }

        var halfSize = data.Size / 2f;
        var distance = SdRoundedBox(local - halfSize, halfSize, data.BorderRadius);
        return new Vector4(finalColor, 1f - SmoothStep(-0.5f, 0.5f, distance));
    }
}
