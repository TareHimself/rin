using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Shared.Math;

namespace Rin.Framework.Views.Graphics.Quads;

public enum PrimitiveType
{
    Line,
    Circle,
    Rectangle,
    QuadraticCurve,
    CubicCurve
}

[StructLayout(LayoutKind.Explicit)]
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


    public required RenderMode Mode
    {
        get => (RenderMode)Opts.X;
        set => Opts.X = (int)value;
    }

    [PublicAPI] [FieldOffset(0)] public Int4 Opts = default;
    [PublicAPI] [FieldOffset(16)] public required Vector2 Size;
    [PublicAPI] [FieldOffset(24)] public required Matrix4x4 Transform;
    [FieldOffset(88)] private unsafe fixed byte _data[16 * 8];

    // [FieldOffset(88)] public PrimitiveData PrimitiveInfo = default;
    
    [FieldOffset(88)] public LineData LineInfo = default;
    
    [FieldOffset(88)] public CircleData CircleInfo = default;
    
    [FieldOffset(88)] public RectangleData RectangleInfo = default;
    
    [FieldOffset(88)] public QuadraticCurveData QuadraticCurveInfo = default;
    
    [FieldOffset(88)] public CubicCurveData CubicCurveInfo = default;

    [FieldOffset(88)] public TextureData TextureInfo = default;

    [FieldOffset(88)] public MtsdfData MtsdfInfo = default;

    public struct LineData
    {
        public Color Color;
        public Vector2 Begin;
        public Vector2 End;
        public float Thickness;
    }
    
    public struct CircleData
    {
        public Matrix4x4 InverseTransform;
        public Color Color;
        public float Radius;
    }
    
    public struct RectangleData
    {
        public Matrix4x4 InverseTransform;
        public Color Color;
        public Vector4 BorderRadius;
    }
    
    public struct QuadraticCurveData
    {
        public Color Color;
        public Vector2 Begin;
        public Vector2 End;
        public Vector2 Control;
        public float Thickness;
    }
    
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

    public struct TextureData
    {
        public ImageHandle ImageHandle { get; set; }
        public Vector4 Tint { get; set; }
        public Vector4 UV { get; set; }
        public Vector4 BorderRadius { get; set; }
    }

    public struct MtsdfData
    {
        public ImageHandle ImageHandle { get; set; }
        public Vector4 Color { get; set; }
        public Vector4 UV { get; set; }
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
                Radius = radius,
            }
        };
        return quad;
    }

    public static Quad Line(in Matrix4x4 transform,in Vector2 begin, in Vector2 end, float thickness = 2.0f, in Color? color = null)
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
                Thickness = thickness,
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
                BorderRadius = borderRadius ?? Vector4.Zero,
            }
        };

        return quad;
    }

    public static Quad QuadraticCurve(in Matrix4x4 transform,in Vector2 a, in Vector2 control, in Vector2 b, float thickness = 2.0f,
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
                Thickness = thickness,
            }
        };

        return quad;
    }

    public static Quad CubicCurve(in Matrix4x4 transform,in Vector2 a, in Vector2 controlA, in Vector2 b, in Vector2 controlB,
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
                Thickness = thickness,
            }
        };

        return quad;
    }

    public static Quad Texture(ImageHandle imageHandle, in Matrix4x4 transform, in Vector2 size, Color? tint = null,
        in Vector4? borderRadius = null, in Vector4? uv = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Texture,
            Transform = transform,
            Size = size,
            TextureInfo = new TextureData
            {
                ImageHandle = imageHandle,
                Tint = tint.GetValueOrDefault(Color.White),
                BorderRadius = borderRadius.GetValueOrDefault(),
                UV = uv.GetValueOrDefault(new Vector4(0.0f, 0.0f, 1.0f, 1.0f))
            }
        };

        return quad;
    }

    public static Quad Mtsdf(ImageHandle imageHandle, in Matrix4x4 transform, in Vector2 size, in Color? color = null,
        in Vector4? uv = null)
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
                UV = uv.GetValueOrDefault(new Vector4(0.0f, 0.0f, 1.0f, 1.0f))
            }
        };

        return quad;
    }
}