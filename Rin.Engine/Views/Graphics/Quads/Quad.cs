using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Math;

namespace Rin.Engine.Views.Graphics.Quads;

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

    [PublicAPI] public Vector4<int> Opts = 0;
    [PublicAPI] public required Vector2 Size;
    [PublicAPI] public required Matrix4x4 Transform;
    private unsafe fixed byte _data[16 * 8];

    // public Vector4 Data1 = new Vector4(0.0f);
    // public Vector4 Data2 = new Vector4(0.0f);
    // public Vector4 Data3 = new Vector4(0.0f);
    // public Vector4 Data4 = new Vector4(0.0f);


    public enum PrimitiveType
    {
        Line,
        Circle,
        Rectangle,
        QuadraticCurve,
        CubicCurve
    }

    private struct PrimitiveData : IQuad
    {
        public Vector4<int> Opts { get; set; }
        public Vector2 Size { get; set; }
        public Matrix4x4 Transform { get; set; }
        public required PrimitiveType Type { get; set; }
        public Vector4 Data1 { get; set; }
        public Vector4 Data2 { get; set; }
        public Vector4 Data3 { get; set; }
        public Vector4 Data4 { get; set; }
    }

    private struct TextureData : IQuad
    {
        public Vector4<int> Opts { get; set; }
        public Vector2 Size { get; set; }
        public Matrix4x4 Transform { get; set; }
        public ImageHandle ImageHandle { get; set; }
        public Vector4 Tint { get; set; }
        public Vector4 UV { get; set; }
        public Vector4 BorderRadius { get; set; }
    }

    private struct MtsdfData : IQuad
    {
        public Vector4<int> Opts { get; set; }
        public Vector2 Size { get; set; }
        public Matrix4x4 Transform { get; set; }
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
            Size = new Vector2(radius)
        };

        unsafe
        {
            var asData = Utils.Reinterpret<PrimitiveData, Quad>(&quad);
            asData->Type = PrimitiveType.Circle;
            asData->Data1 = color.GetValueOrDefault(Color.White);
            asData->Data2 = new Vector4(radius, 0.0f, 0.0f, 0.0f);
        }

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
            Size = size + thicknessVector * 2.0f
        };

        unsafe
        {
            var asData = Utils.Reinterpret<PrimitiveData, Quad>(&quad);
            asData->Type = PrimitiveType.Line;
            asData->Data1 = color.GetValueOrDefault(Color.White);
            asData->Data2 = new Vector4(begin, end.X, end.Y);
            asData->Data3 = new Vector4(thickness);
        }

        return quad;
    }

    public static Quad Rect(in Matrix4x4 transform, in Vector2 size, in Color? color = null,
        in Vector4? borderRadius = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Primitive,
            Transform = transform,
            Size = size
        };

        unsafe
        {
            var asData = Utils.Reinterpret<PrimitiveData, Quad>(&quad);
            asData->Type = PrimitiveType.Rectangle;
            asData->Data1 = color.GetValueOrDefault(Color.White);
            asData->Data2 = borderRadius.GetValueOrDefault();
        }

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
            Size = size + thicknessVector * 2.0f
        };

        unsafe
        {
            var asData = Utils.Reinterpret<PrimitiveData, Quad>(&quad);
            asData->Type = PrimitiveType.QuadraticCurve;
            asData->Data1 = color.GetValueOrDefault(Color.White);
            asData->Data2 = new Vector4(a, b.X, b.Y);
            asData->Data3 = new Vector4(control, thickness, 0);
        }

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
            Size = size + thicknessVector * 2.0f
        };
        unsafe
        {
            var asData = Utils.Reinterpret<PrimitiveData, Quad>(&quad);
            asData->Type = PrimitiveType.CubicCurve;
            asData->Data1 = color.GetValueOrDefault(Color.White);
            asData->Data2 = new Vector4(a, b.X, b.Y);
            asData->Data3 = new Vector4(controlA, controlB.X, controlB.Y);
            asData->Data4 = new Vector4(thickness, 0, 0, 0);
        }

        return quad;
    }

    public static Quad Texture(ImageHandle imageHandle, in Matrix4x4 transform, in Vector2 size, Color? tint = null,
        in Vector4? borderRadius = null, in Vector4? uv = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Texture,
            Transform = transform,
            Size = size
        };

        unsafe
        {
            var asData = Utils.Reinterpret<TextureData, Quad>(&quad);
            asData->ImageHandle = imageHandle;
            asData->Tint = tint.GetValueOrDefault(Color.White);
            asData->BorderRadius = borderRadius.GetValueOrDefault();
            asData->UV = uv.GetValueOrDefault(new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
        }

        return quad;
    }

    public static Quad Mtsdf(ImageHandle imageHandle, in Matrix4x4 transform, in Vector2 size, in Color? color = null,
        in Vector4? uv = null)
    {
        var quad = new Quad
        {
            Mode = RenderMode.Mtsdf,
            Transform = transform,
            Size = size
        };

        unsafe
        {
            var asData = Utils.Reinterpret<MtsdfData, Quad>(&quad);
            asData->ImageHandle = imageHandle;
            asData->Color = color.GetValueOrDefault(Color.White);
            asData->UV = uv.GetValueOrDefault(new Vector4(0.0f, 0.0f, 1.0f, 1.0f));
        }

        return quad;
    }


    // public Quad Clone()
    // {
    //     var quad = new Quad()
    //     {
    //         Mode = Mode,
    //         Opts = Opts.Clone(),
    //         Size = Size.Clone(),
    //         Transform = Transform.Clone(),
    //     };
    //     q
    //     unsafe
    //     {
    //         fixed (byte* src = _data)
    //         {
    //
    //             Marshal.Copy(new IntPtr(src),);
    //             quad._data = _data;
    //         }
    //     }
    //     return 
    // }
}