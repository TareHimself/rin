using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views.Graphics.Quads;
using Rin.Shade;

namespace Rin.Core.Views.Graphics.Shaders;

[Shader("Shaders/Rin/Core/Views/quad_batch.slang")]
public partial class QuadBatchShader : Shader
{
    public struct PushConstants
    {
        public Matrix4x4 Projection;
        public Vector4 Viewport;
        public BufferRef<Quad> Quads;
    }

    public struct VertexIn
    {
        [InstanceId] public int InstanceId;
        [VertexId] public int VertexId;
    }

    public struct VertexOut
    {
        [Semantic("UV")] public Vector2 Uv;
        [Semantic("QUAD_INDEX")] public int QuadIndex;
        [Position] public Vector4 Position;
    }

    public struct FragmentIn
    {
        [Semantic("UV")] public Vector2 Uv;
        [Semantic("QUAD_INDEX")] public int QuadIndex;
        [Position] public Vector2 Coordinate;
    }

    [Push] protected PushConstants Push;
    protected static BindlessData Bindless;

    protected override BlendState BlendState => BlendState.Alpha;

    [Vertex]
    public VertexOut Vertex(VertexIn input)
    {
        var quad = Push.Quads[input.InstanceId];
        var corners = new[]
        {
            new Vector2(0f, 0f), new Vector2(quad.Size.X, 0f), new Vector2(quad.Size.X, quad.Size.Y),
            new Vector2(0f, 0f), new Vector2(quad.Size.X, quad.Size.Y), new Vector2(0f, quad.Size.Y)
        };

        var corner = corners[input.VertexId];
        var projected = Vector4.Transform(Vector4.Transform(new Vector4(corner, 0f, 1f), quad.Transform), Push.Projection);

        VertexOut output;
        output.QuadIndex = input.InstanceId;
        output.Position = new Vector4(projected.X, projected.Y, 0f, 1f);
        output.Uv = corner / quad.Size;
        return output;
    }

    private static Vector4 ColorOf(Color color)
    {
        return new Vector4(color.R, color.G, color.B, color.A);
    }

    private static Vector4 Coverage(Vector4 color, float distance)
    {
        var aaWidth = 0.5f * Math.Max(Fwidth(distance), 1e-6f);
        if (distance > aaWidth) Discard();

        var alpha = Math.SmoothStep(-aaWidth, aaWidth, distance);
        return Math.Lerp(color, new Vector4(color.xyz, 0f), alpha);
    }

    private static Vector2 AtlasUv(Vector2 uv, Vector4 mapping)
    {
        return mapping.xy + (mapping.zw - mapping.xy) * uv;
    }

    [Fragment, Attachment(AttachmentFormat.RGBA16), Stencil]
    public Vector4 Fragment(FragmentIn input)
    {
        var quad = Push.Quads[input.QuadIndex];
        var mode = quad.Mode;

        switch (mode)
        {
            case Quad.RenderMode.Line:
            {
                var data = quad.LineInfo;
                var distance = Sd.Segment(input.Coordinate, data.Begin, data.End) - data.Thickness * 0.5f;
                return Coverage(ColorOf(data.Color), distance);
            }

            case Quad.RenderMode.Circle:
            {
                var data = quad.CircleInfo;
                var local = ViewShaderMath.TransformPoint(input.Coordinate, data.InverseTransform);
                var distance = Sd.Circle(local, new Vector2(data.Radius), data.Radius);
                return Coverage(ColorOf(data.Color), distance);
            }

            case Quad.RenderMode.Rectangle:
            {
                var data = quad.RectangleInfo;
                var local = ViewShaderMath.TransformPoint(input.Coordinate, data.InverseTransform);
                var halfSize = quad.Size / 2f;
                var distance = ViewShaderMath.SdRoundedBox(local - halfSize, halfSize, data.BorderRadius);
                return Coverage(ColorOf(data.Color), distance);
            }

            case Quad.RenderMode.QuadraticCurve:
            {
                var data = quad.QuadraticCurveInfo;
                var distance = Sd.Bezier(input.Coordinate, data.Begin, data.End, data.Control) - data.Thickness * 0.5f;
                return Coverage(ColorOf(data.Color), distance);
            }

            case Quad.RenderMode.CubicCurve:
            {
                var data = quad.CubicCurveInfo;
                var distance = Sd.Cubic(input.Coordinate, data.Begin, data.End, data.ControlA, data.ControlB) -
                               data.Thickness * 0.5f;
                return Coverage(ColorOf(data.Color), distance);
            }

            case Quad.RenderMode.Texture:
            {
                var data = quad.TextureInfo;
                var uv = AtlasUv(input.Uv, data.UV);
                var color = Bindless.SampleTexture(data.ImageHandle, uv, ImageTiling.Repeat, ImageFilter.Linear) * data.Tint;
                return ViewShaderMath.ApplyBorderRadius(input.Coordinate, color, data.BorderRadius, quad.Size,
                    data.InverseTransform, 1f);
            }

            case Quad.RenderMode.Mtsdf:
            {
                var data = quad.MtsdfInfo;
                var uv = AtlasUv(input.Uv, data.UV);
                var msd = Bindless.SampleTexture(data.ImageHandle, uv, ImageTiling.ClampEdge, ImageFilter.Linear).xyz;
                var textureSize = Bindless.GetTextureSize(data.ImageHandle);
                var actualSize = textureSize * (data.UV.zw - data.UV.xy);

                var sd = ViewShaderMath.Median(msd.X, msd.Y, msd.Z);
                var distance = ViewShaderMath.ScreenPxRange(input.Uv, actualSize, data.PixelRange) * (sd - 0.5f);
                var opacity = 1f - Math.Clamp(distance + 0.5f, 0f, 1f);
                return new Vector4(data.Color.xyz, data.Color.W * opacity);
            }

            case Quad.RenderMode.ColorWheel:
            {
                var data = quad.ColorWheelInfo;
                var centered = input.Uv * 2f - new Vector2(1f);
                var radius = Math.Sqrt(centered.X * centered.X + centered.Y * centered.Y);
                const float pi = 3.14159265359f;
                var hue = (Math.Atan2(centered.Y, centered.X) + pi) / (pi * 2f);
                var rgb = ViewShaderMath.Hsv2Rgb(new Vector3(hue, radius, 1f));
                return ViewShaderMath.ApplyBorderRadius(input.Coordinate, new Vector4(rgb, 1f), new Vector4(quad.Size.X / 2f),
                    quad.Size, data.InverseTransform, 1f);
            }

            default:
                return new Vector4(1f);
        }
    }
}
