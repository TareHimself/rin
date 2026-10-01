using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;

namespace ViewsTest.Shaders;

[NoReorder]
public struct PrettyData
{
    public Matrix4x4 Projection;
    public Vector2 ScreenSize;
    public Matrix4x4 Transform;
    public Vector2 Size;
    public float Time;
    public Vector2 Cursor;
}

[Shader("Shaders/ViewsTest/pretty.slang")]
public partial class PrettyShader : Shader
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

    protected override BlendState BlendState => BlendState.Alpha;

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
        return a + b * Math.Cos(6.28318f * (c * t + d));
    }

    // https://www.shadertoy.com/view/mtyGWy
    [Fragment, Stencil, Attachment(AttachmentFormat.RGBA16)]
    public Vector4 Fragment(FragmentIn input)
    {
        var data = Push.Data[0];
        var size = data.Size;
        var uv = input.Coordinate / size;
        uv -= new Vector2(data.Cursor.X / size.X, data.Cursor.Y / size.Y);
        var uv0 = uv;

        var finalColor = new Vector3(0f);
        for (var i = 0f; i < 4f; i++)
        {
            uv = Math.Frac(uv * 1.5f) - new Vector2(0.5f);

            var d = Math.Length(uv) * Math.Exp(Math.Length(uv0) * -1f);
            var color = Palette(Math.Length(uv0) + i * 0.4f + data.Time * 0.4f);

            d = Math.Sin(d * 8f + data.Time) / 8f;
            d = Math.Abs(d);
            d = Math.Pow(0.01f / d, 1.2f);

            finalColor += color * d;
        }

        return new Vector4(finalColor, 1f);
    }
}
