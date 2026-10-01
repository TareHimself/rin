using System.Numerics;
using experiment.Slug.Rendering;
using Rin.Core.Graphics;
using Rin.Shade;

namespace experiment.Slug.Shaders;

// Port of Eric Lengyel's SLUG reference implementation (https://github.com/EricLengyel/Slug, MIT/Apache-2.0).
[Shader("Shaders/Slug/slug.slang")]
public partial class SlugShader : Shader
{
    private const int LogBandWidth = 12;
    private const int BandWidth = 4096;

    public struct PushConstants
    {
        public BufferRef<SlugInstanceData> Instances;
        public DeviceHandle CurveTexture;
        public DeviceHandle BandTexture;
        public Matrix4x4 Projection;
    }

    public struct VertexIn
    {
        [VertexId] public int VertexId;
        [InstanceId] public int InstanceId;
    }

    public struct VertexOut
    {
        [Semantic("EM_COORD")] public Vector2 EmCoord;
        [Semantic("INSTANCE_ID")] public int InstanceId;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("EM_COORD")] public Vector2 EmCoord;
        [Semantic("INSTANCE_ID")] public int InstanceId;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    protected override BlendState BlendState => BlendState.Alpha;

    private static int BandLocX(int shapeLocX, int offset)
    {
        return (shapeLocX + offset) & (BandWidth - 1);
    }

    private static int BandLocY(int shapeLocX, int shapeLocY, int offset)
    {
        return shapeLocY + ((shapeLocX + offset) >> LogBandWidth);
    }

    private static uint CalcRootCode(float y1, float y2, float y3)
    {
        var i1 = AsUInt(y1) >> 31;
        var i2 = AsUInt(y2) >> 30;
        var i3 = AsUInt(y3) >> 29;
        var shift = (i2 & 2u) | (i1 & ~2u);
        shift = (i3 & 4u) | (shift & ~4u);
        return (0x2E74u >> (int)shift) & 0x0101u;
    }

    private static Vector2 SolveHorizPoly(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var a = p1 - p2 * 2f + p3;
        var b = p1 - p2;
        var ra = 1f / a.Y;
        var rb = 0.5f / b.Y;

        var d = Math.Sqrt(Math.Max(b.Y * b.Y - a.Y * p1.Y, 0f));
        var t1 = (b.Y - d) * ra;
        var t2 = (b.Y + d) * ra;

        if (Math.Abs(a.Y) < 1f / 65536f)
        {
            t1 = p1.Y * rb;
            t2 = t1;
        }

        return new Vector2((a.X * t1 - b.X * 2f) * t1 + p1.X, (a.X * t2 - b.X * 2f) * t2 + p1.X);
    }

    private static Vector2 SolveVertPoly(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        var a = p1 - p2 * 2f + p3;
        var b = p1 - p2;
        var ra = 1f / a.X;
        var rb = 0.5f / b.X;

        var d = Math.Sqrt(Math.Max(b.X * b.X - a.X * p1.X, 0f));
        var t1 = (b.X - d) * ra;
        var t2 = (b.X + d) * ra;

        if (Math.Abs(a.X) < 1f / 65536f)
        {
            t1 = p1.X * rb;
            t2 = t1;
        }

        return new Vector2((a.Y * t1 - b.Y * 2f) * t1 + p1.Y, (a.Y * t2 - b.Y * 2f) * t2 + p1.Y);
    }

    private static float CalcCoverage(float xcov, float ycov, float xwgt, float ywgt)
    {
        var coverage = Math.Max(Math.Abs(xcov * xwgt + ycov * ywgt) / Math.Max(xwgt + ywgt, 1f / 65536f),
            Math.Min(Math.Abs(xcov), Math.Abs(ycov)));
        return Math.Saturate(coverage);
    }

    private float SlugRender(Vector2 emCoord, Vector4 banding, int shapeLocX, int shapeLocY, int bandMaxX,
        int bandMaxY, DeviceHandle curveTexture, DeviceHandle bandTexture)
    {
        var pixelsPerEm = new Vector2(1f) / Math.Max(Fwidth(emCoord), new Vector2(1e-6f));
        var bandPosition = emCoord * banding.xy + banding.zw;
        var bandIndexX = (int)Math.Clamp(bandPosition.X, 0f, (float)bandMaxX);
        var bandIndexY = (int)Math.Clamp(bandPosition.Y, 0f, (float)bandMaxY);
        var emCoord4 = new Vector4(emCoord.X, emCoord.Y, emCoord.X, emCoord.Y);

        var xcov = 0f;
        var xwgt = 0f;

        {
            var header = Bindless.TexelLoad(bandTexture, shapeLocX + bandIndexY, shapeLocY).xy;
            var count = (int)header.X;
            var listOffset = (int)header.Y;

            for (var i = 0; i < count; i++)
            {
                var entry = Bindless.TexelLoad(bandTexture, BandLocX(shapeLocX, listOffset) + i,
                    BandLocY(shapeLocX, shapeLocY, listOffset));
                var curveX = (int)entry.X;
                var curveY = (int)entry.Y;

                var c01 = Bindless.TexelLoad(curveTexture, curveX, curveY) - emCoord4;
                var p3 = Bindless.TexelLoad(curveTexture, curveX + 1, curveY).xy - emCoord;
                var p1 = c01.xy;
                var p2 = c01.zw;

                if (Math.Max(Math.Max(p1.X, p2.X), p3.X) * pixelsPerEm.X < -0.5f) break;

                var code = CalcRootCode(p1.Y, p2.Y, p3.Y);
                if (code != 0u)
                {
                    var r = SolveHorizPoly(p1, p2, p3) * pixelsPerEm.X;
                    if ((code & 1u) != 0u)
                    {
                        xcov += Math.Saturate(r.X + 0.5f);
                        xwgt = Math.Max(xwgt, Math.Saturate(1f - Math.Abs(r.X) * 2f));
                    }

                    if (code > 1u)
                    {
                        xcov -= Math.Saturate(r.Y + 0.5f);
                        xwgt = Math.Max(xwgt, Math.Saturate(1f - Math.Abs(r.Y) * 2f));
                    }
                }
            }
        }

        var ycov = 0f;
        var ywgt = 0f;

        {
            var header = Bindless.TexelLoad(bandTexture, shapeLocX + bandMaxY + 1 + bandIndexX, shapeLocY).xy;
            var count = (int)header.X;
            var listOffset = (int)header.Y;

            for (var i = 0; i < count; i++)
            {
                var entry = Bindless.TexelLoad(bandTexture, BandLocX(shapeLocX, listOffset) + i,
                    BandLocY(shapeLocX, shapeLocY, listOffset));
                var curveX = (int)entry.X;
                var curveY = (int)entry.Y;

                var c01 = Bindless.TexelLoad(curveTexture, curveX, curveY) - emCoord4;
                var p3 = Bindless.TexelLoad(curveTexture, curveX + 1, curveY).xy - emCoord;
                var p1 = c01.xy;
                var p2 = c01.zw;

                if (Math.Max(Math.Max(p1.Y, p2.Y), p3.Y) * pixelsPerEm.Y < -0.5f) break;

                var code = CalcRootCode(p1.X, p2.X, p3.X);
                if (code != 0u)
                {
                    var r = SolveVertPoly(p1, p2, p3) * pixelsPerEm.Y;
                    if ((code & 1u) != 0u)
                    {
                        ycov -= Math.Saturate(r.X + 0.5f);
                        ywgt = Math.Max(ywgt, Math.Saturate(1f - Math.Abs(r.X) * 2f));
                    }

                    if (code > 1u)
                    {
                        ycov += Math.Saturate(r.Y + 0.5f);
                        ywgt = Math.Max(ywgt, Math.Saturate(1f - Math.Abs(r.Y) * 2f));
                    }
                }
            }
        }

        return CalcCoverage(xcov, ycov, xwgt, ywgt);
    }

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var instance = Push.Instances[input.InstanceId];
        var corners = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f),
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
        };

        var corner = corners[input.VertexId];
        var position = instance.MinPos + (instance.MaxPos - instance.MinPos) * corner;

        VertexOut output;
        output.Position = Vector4.Transform(new Vector4(position, 0f, 1f), Push.Projection);
        output.EmCoord = instance.MinEm + (instance.MaxEm - instance.MinEm) * corner;
        output.InstanceId = input.InstanceId;
        return output;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA32)]
    public Vector4 Fragment(FragmentIn input)
    {
        var instance = Push.Instances[input.InstanceId];
        var coverage = SlugRender(input.EmCoord, instance.Banding, instance.ShapeLocX, instance.ShapeLocY,
            instance.BandMaxX, instance.BandMaxY, Push.CurveTexture, Push.BandTexture);

        return instance.Color * coverage;
    }
}
