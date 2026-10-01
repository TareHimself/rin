using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Graphics.Quads;

public enum PrimitiveType
{
    Line,
    Circle,
    Rectangle,
    QuadraticCurve,
    CubicCurve
}

[StructLayout(LayoutKind.Explicit)]
[NoReorder]
public struct Quad() // : ICloneable<Quad>
{
    public enum RenderMode
    {
        Line,
        Circle,
        Rectangle,
        QuadraticCurve,
        CubicCurve,
        Texture,
        Mtsdf,
        ColorWheel
    }


    [PublicAPI] [FieldOffset(0)] public required RenderMode Mode;
    [PublicAPI] [FieldOffset(4)] public required Vector2 Size;
    [PublicAPI] [FieldOffset(12)] public required Matrix4x4 Transform;

    // [FieldOffset(88)] public PrimitiveData PrimitiveInfo = default;

    [FieldOffset(76)] public LineData LineInfo = default;

    [FieldOffset(76)] public CircleData CircleInfo = default;

    [FieldOffset(76)] public RectangleData RectangleInfo = default;

    [FieldOffset(76)] public QuadraticCurveData QuadraticCurveInfo = default;

    [FieldOffset(76)] public CubicCurveData CubicCurveInfo = default;

    [FieldOffset(76)] public TextureData TextureInfo = default;

    [FieldOffset(76)] public MtsdfData MtsdfInfo = default;

    [FieldOffset(76)] public ColorWheelData ColorWheelInfo = default;

    [NoReorder]
    public struct LineData
    {
        public Color Color;
        public Vector2 Begin;
        public Vector2 End;
        public float Thickness;
    }

    [NoReorder]
    public struct CircleData
    {
        public Matrix4x4 InverseTransform;
        public Color Color;
        public float Radius;
    }

    [NoReorder]
    public struct RectangleData
    {
        public Matrix4x4 InverseTransform;
        public Color Color;
        public Vector4 BorderRadius;
    }

    [NoReorder]
    public struct QuadraticCurveData
    {
        public Color Color;
        public Vector2 Begin;
        public Vector2 End;
        public Vector2 Control;
        public float Thickness;
    }

    [NoReorder]
    public struct CubicCurveData
    {
        public Color Color;
        public Vector2 Begin;
        public Vector2 End;
        public Vector2 ControlA;
        public Vector2 ControlB;
        public float Thickness;
    }

    // public struct PrimitiveData
    // {
    //     public PrimitiveType Type { get; set; }
    //     public Vector4 Data1 { get; set; }
    //     public Vector4 Data2 { get; set; }
    //     public Vector4 Data3 { get; set; }
    //     public Vector4 Data4 { get; set; }
    // }
    [NoReorder]
    public struct ColorWheelData
    {
        public Matrix4x4 InverseTransform;
    }

    [NoReorder]
    public struct TextureData
    {
        public Matrix4x4 InverseTransform;
        public DeviceHandle ImageHandle;
        public Vector4 Tint;
        public Vector4 UV;
        public Vector4 BorderRadius;
    }

    [NoReorder]
    public struct MtsdfData
    {
        public DeviceHandle ImageHandle;
        public Vector4 Color;
        public Vector4 UV;

        /// <summary>
        ///     The pixel range (padding baked into the SDF around each edge for anti-aliasing) the atlas image
        ///     was generated with - must match the shader's screen-pixel-range calculation exactly, so this
        ///     travels with the quad instead of being a hardcoded shader constant.
        /// </summary>
        public float PixelRange;
    }

    public static Quad Circle(in Matrix4x4 transform, float radius, in Color? color = null)
    {
        var size = new Vector2(radius * 2f);
        var quad = new Quad
        {
            Mode = RenderMode.Circle,
            Transform = transform,
            Size = size,
            CircleInfo = new CircleData
            {
                InverseTransform = transform.Inverse(),
                Color = color ?? Color.White,
                Radius = radius
            }
        };
        return quad;
    }

    public static Quad Line(in Matrix4x4 transform, in Vector2 begin, in Vector2 end, float thickness = 2.0f,
        in Color? color = null)
    {
        var p1 = Vector2.Min(begin, end);
        var p2 = Vector2.Max(begin, end);

        var thicknessVector = new Vector2(thickness);
        p1 -= thicknessVector;
        p2 += thicknessVector;

        var size = p2 - p1;
        var t = Matrix4x4.Identity.Translate(p1).ChildOf(transform);
        var quad = new Quad
        {
            Mode = RenderMode.Line,
            Transform = t,
            Size = size,
            // LineInfo = new LineData
            // {
            //     Begin = (begin - p1) / size,
            //     End = (end - p1) / size,
            //     Color = color ?? Color.White,
            //     Thickness = thickness / float.Min(size.X,size.Y),
            // }
            LineInfo = new LineData
            {
                Begin = begin.Transform(transform),
                End = end.Transform(transform),
                Color = color ?? Color.White,
                Thickness = thickness
            }
        };

        return quad;
    }

    public static Quad Rect(in Matrix4x4 transform, in Vector2 size, in Color? color = null,
        in Vector4? borderRadius = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Rectangle,
            Transform = transform,
            Size = size,
            RectangleInfo = new RectangleData
            {
                InverseTransform = transform.Inverse(),
                Color = color ?? Color.White,
                BorderRadius = borderRadius ?? Vector4.Zero
            }
        };

        return quad;
    }

    public static Quad QuadraticCurve(in Matrix4x4 transform, in Vector2 a, in Vector2 control, in Vector2 b,
        float thickness = 2.0f,
        in Color? color = null)
    {
        var p1 = Vector2.Min(control, Vector2.Min(a, b));
        var p2 = Vector2.Max(control, Vector2.Max(a, b));

        var thicknessVector = new Vector2(thickness);

        p1 -= thicknessVector;
        p2 += thicknessVector;

        var size = p2 - p1;
        var quad = new Quad
        {
            Mode = RenderMode.QuadraticCurve,
            Transform = Matrix4x4.Identity.Translate(p1).ChildOf(transform),
            Size = size,
            QuadraticCurveInfo = new QuadraticCurveData
            {
                Begin = a.Transform(transform),
                End = b.Transform(transform),
                Control = control.Transform(transform),
                Color = color ?? Color.White,
                Thickness = thickness
            }
        };

        return quad;
    }

    public static Quad CubicCurve(in Matrix4x4 transform, in Vector2 a, in Vector2 controlA, in Vector2 b,
        in Vector2 controlB,
        float thickness = 2.0f,
        in Color? color = null)
    {
        var p1 = Vector2.Min(Vector2.Min(controlA, controlB), Vector2.Min(a, b));
        var p2 = Vector2.Max(Vector2.Max(controlA, controlB), Vector2.Max(a, b));

        var thicknessVector = new Vector2(thickness);

        p1 -= thicknessVector;
        p2 += thicknessVector;

        var size = p2 - p1;
        var quad = new Quad
        {
            Mode = RenderMode.CubicCurve,
            Transform = Matrix4x4.Identity.Translate(p1).ChildOf(transform),
            Size = size,
            CubicCurveInfo = new CubicCurveData
            {
                Begin = a.Transform(transform),
                End = b.Transform(transform),
                ControlA = controlA.Transform(transform),
                ControlB = controlB.Transform(transform),
                Color = color ?? Color.White,
                Thickness = thickness
            }
        };

        return quad;
    }

    public static Quad Texture(ResourceHandle imageHandle, in Matrix4x4 transform, in Vector2 size, Color? tint = null,
        in Vector4? borderRadius = null, in Vector4? uv = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Texture,
            Transform = transform,
            Size = size,
            TextureInfo = new TextureData
            {
                InverseTransform = transform.Inverse(),
                ImageHandle = imageHandle,
                Tint = tint.GetValueOrDefault(Color.White),
                BorderRadius = borderRadius.GetValueOrDefault(),
                UV = uv.GetValueOrDefault(new Vector4(0.0f, 0.0f, 1.0f, 1.0f))
            }
        };

        return quad;
    }

    public static Quad ColorWheel(in Matrix4x4 transform, in Vector2 size)
    {
        return new Quad
        {
            Mode = RenderMode.ColorWheel,
            Transform = transform,
            Size = size,
            ColorWheelInfo = new ColorWheelData
            {
                InverseTransform = transform.Inverse()
            }
        };
    }

    public static Quad Mtsdf(ResourceHandle imageHandle, in Matrix4x4 transform, in Vector2 size, float pixelRange,
        in Color? color = null, in Vector4? uv = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Mtsdf,
            Transform = transform,
            Size = size,
            MtsdfInfo = new MtsdfData
            {
                ImageHandle = imageHandle,
                Color = color.GetValueOrDefault(Color.White),
                UV = uv.GetValueOrDefault(new Vector4(0.0f, 0.0f, 1.0f, 1.0f)),
                PixelRange = pixelRange
            }
        };

        return quad;
    }
}