using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics.Quads;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Quad(Mat3 transform, Vec2<float> size) : ICloneable<Quad>
{
    public enum RenderMode
    {
        Shape,
        Texture,
        Sdf,
        ColorWheel
    }
    
    public int TextureId
    {
        get => Opts.Y;
        init => Opts.Y = value;
    }
    
    public RenderMode Mode
    {
        get => (RenderMode)Opts.X;
        set => Opts.X = (int)value;
    }
    
    public Vec4<int> Opts = new Vec4<int>(0,0,0,0);
    public Vec2<float> Size = size; 
    public Mat3 Transform = transform;
    public Vec4<float> Data1 = new Vec4<float>(0.0f);
    public Vec4<float> Data2 = new Vec4<float>(0.0f);
    public Vec4<float> Data3 = new Vec4<float>(0.0f);
    public Vec4<float> Data4 = new Vec4<float>(0.0f);

    public static Quad NewRect(Mat3 transform, Vec2<float> size,Vec4<float>? color = null,Vec4<float>? borderRadius = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Shape,
            Data1 = color.GetValueOrDefault(1.0f),
            Data2 = borderRadius.GetValueOrDefault(0.0f)
        };
    }
    
    public static Quad NewTexture(int textureId,Mat3 transform, Vec2<float> size,Vec4<float>? tint = null,Vec4<float>? borderRadius = null,Vec4<float>? uv = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Texture,
            TextureId = textureId,
            Data1 = tint.GetValueOrDefault(1.0f),
            Data2 = uv.GetValueOrDefault(new Vec4<float>(0.0f,0.0f,1.0f,1.0f)),
            Data3 = borderRadius.GetValueOrDefault(0.0f)
        };
    }
    
    public static Quad NewSdf(int textureId, Mat3 transform, Vec2<float> size, Vec4<float>? color = null,
        Vec4<float>? uv = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Sdf,
            TextureId = textureId,
            Data1 = color.GetValueOrDefault(1.0f),
            Data2 = uv.GetValueOrDefault(new Vec4<float>(0.0f,0.0f,1.0f,1.0f)),
        };
    }

    public Quad Clone()
    {
        return new Quad(Transform.Clone(), Size.Clone())
        {
            Opts = Opts.Clone(),
            Data1 = Data1.Clone(),
            Data2 = Data2.Clone(),
            Data3 = Data3.Clone(),
            Data4 = Data4.Clone()
        };
    }
}