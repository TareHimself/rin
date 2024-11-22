using System.Runtime.InteropServices;
using rin.Core;
using rin.Core.Math;

namespace rin.Widgets.Graphics.Quads;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Quad(Matrix3 transform, Vector2<float> size) : ICloneable<Quad>
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
    
    public Vector4<int> Opts = new Vector4<int>(0,0,0,0);
    public Vector2<float> Size = size; 
    public Matrix3 Transform = transform;
    public Vector4<float> Data1 = new Vector4<float>(0.0f);
    public Vector4<float> Data2 = new Vector4<float>(0.0f);
    public Vector4<float> Data3 = new Vector4<float>(0.0f);
    public Vector4<float> Data4 = new Vector4<float>(0.0f);

    public static Quad NewRect(Matrix3 transform, Vector2<float> size,Vector4<float>? color = null,Vector4<float>? borderRadius = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Shape,
            Data1 = color.GetValueOrDefault(1.0f),
            Data2 = borderRadius.GetValueOrDefault(0.0f)
        };
    }
    
    public static Quad NewTexture(int textureId,Matrix3 transform, Vector2<float> size,Vector4<float>? tint = null,Vector4<float>? borderRadius = null,Vector4<float>? uv = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Texture,
            TextureId = textureId,
            Data1 = tint.GetValueOrDefault(1.0f),
            Data2 = uv.GetValueOrDefault(new Vector4<float>(0.0f,0.0f,1.0f,1.0f)),
            Data3 = borderRadius.GetValueOrDefault(0.0f)
        };
    }
    
    public static Quad NewSdf(int textureId, Matrix3 transform, Vector2<float> size, Vector4<float>? color = null,
        Vector4<float>? uv = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Sdf,
            TextureId = textureId,
            Data1 = color.GetValueOrDefault(1.0f),
            Data2 = uv.GetValueOrDefault(new Vector4<float>(0.0f,0.0f,1.0f,1.0f)),
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