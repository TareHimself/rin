using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Math;

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
        Primitive,
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

    [FieldOffset(88)] public PrimitiveData PrimitiveInfo = default;

    [FieldOffset(88)] public TextureData TextureInfo = default;

    [FieldOffset(88)] public MtsdfData MtsdfInfo = default;

    public struct PrimitiveData
    {
        public PrimitiveType Type { get; set; }
        public Vector4 Data1 { get; set; }
        public Vector4 Data2 { get; set; }
        public Vector4 Data3 { get; set; }
        public Vector4 Data4 { get; set; }
    }

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
        var quad = new Quad
        {
            Mode = RenderMode.Primitive,
            Transform = transform,
            Size = new Vector2(radius),
            PrimitiveInfo = new PrimitiveData
            {
                Type = PrimitiveType.Circle,
                Data1 = color.GetValueOrDefault(Color.White),
                Data2 = new Vector4(radius, 0.0f, 0.0f, 0.0f)
            }
        };
        return quad;
    }

    public static Quad Line(in Vector2 begin, in Vector2 end, float thickness = 2.0f, in Color? color = null)
    {
        const float feather = 3.0f;
        var p1 = Vector2.Min(begin, end);
        var p2 = Vector2.Max(begin, end);
        var size = p2 - p1;
        var thicknessVector = new Vector2(thickness + feather);
        p1 -= thicknessVector;

        var quad = new Quad
        {
            Mode = RenderMode.Primitive,
            Transform = Matrix4x4.Identity.Translate(p1),
            Size = size + thicknessVector * 2.0f,
            PrimitiveInfo = new PrimitiveData
            {
                Type = PrimitiveType.Line,
                Data1 = color.GetValueOrDefault(Color.White),
                Data2 = new Vector4(begin, end.X, end.Y),
                Data3 = new Vector4(thickness)
            }
        };

        return quad;
    }

    public static Quad Rect(in Matrix4x4 transform, in Vector2 size, in Color? color = null,
        in Vector4? borderRadius = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Primitive,
            Transform = transform,
            Size = size,
            PrimitiveInfo = new PrimitiveData
            {
                Type = PrimitiveType.Rectangle,
                Data1 = color.GetValueOrDefault(Color.White),
                Data2 = borderRadius.GetValueOrDefault()
            }
        };

        return quad;
    }

    public static Quad QuadraticCurve(in Vector2 a, in Vector2 control, in Vector2 b, float thickness = 2.0f,
        in Color? color = null)
    {
        const float feather = 3.0f;
        var p1 = Vector2.Min(control, Vector2.Min(a, b));
        var p2 = Vector2.Max(control, Vector2.Max(a, b));
        var size = p2 - p1;
        var thicknessVector = new Vector2(thickness + feather);
        p1 -= thicknessVector;
        var quad = new Quad
        {
            Mode = RenderMode.Primitive,
            Transform = Matrix4x4.Identity.Translate(p1),
            Size = size + thicknessVector * 2.0f,
            PrimitiveInfo = new PrimitiveData
            {
                Type = PrimitiveType.QuadraticCurve,
                Data1 = color.GetValueOrDefault(Color.White),
                Data2 = new Vector4(a, b.X, b.Y),
                Data3 = new Vector4(control, thickness, 0)
            }
        };

        return quad;
    }

    public static Quad CubicCurve(in Vector2 a, in Vector2 controlA, in Vector2 b, in Vector2 controlB,
        float thickness = 2.0f,
        in Color? color = null)
    {
        const float feather = 3.0f;
        var p1 = Vector2.Min(Vector2.Min(controlA, controlB), Vector2.Min(a, b));
        var p2 = Vector2.Max(Vector2.Max(controlA, controlB), Vector2.Max(a, b));
        var size = p2 - p1;
        var midpoint = p1 + size / 2.0f;
        var thicknessVector = new Vector2(thickness + feather);
        p1 -= thicknessVector;
        var quad = new Quad
        {
            Mode = RenderMode.Primitive,
            Transform = Matrix4x4.Identity.Translate(p1),
            Size = size + thicknessVector * 2.0f,
            PrimitiveInfo = new PrimitiveData
            {
                Type = PrimitiveType.CubicCurve,
                Data1 = color.GetValueOrDefault(Color.White),
                Data2 = new Vector4(a, b.X, b.Y),
                Data3 = new Vector4(controlA, controlB.X, controlB.Y),
                Data4 = new Vector4(thickness, 0, 0, 0)
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