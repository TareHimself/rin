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
    
    public RenderMode Mode
    {
        get => (RenderMode)Opts.X;
        set => Opts.X = (int)value;
    }
    
    public Vec4<int> Opts = 0;
    public Vec2<float> Size = size; 
    public Mat3 Transform = transform;
    public Vec4<float> Data1 = 0.0f;
    public Vec4<float> Data2 = 0.0f;
    public Vec4<float> Data3 = 0.0f;
    public Vec4<float> Data4 = 0.0f;

    public static Quad Rect(Mat3 transform, Vec2<float> size,Vec4<float>? color = null,Vec4<float>? borderRadius = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Shape,
            Data1 = color.GetValueOrDefault(1.0f),
            Data2 = borderRadius.GetValueOrDefault(0.0f)
        };
    }
    
    public static Quad Texture(int textureId,Mat3 transform, Vec2<float> size,Vec4<float>? tint = null,Vec4<float>? borderRadius = null,Vec4<float>? uv = null)
    {
        var quad = new Quad(transform, size)
        {
            Mode = RenderMode.Texture,
            Data1 = tint.GetValueOrDefault(1.0f),
            Data2 = uv.GetValueOrDefault(new Vec4<float>(0.0f,0.0f,1.0f,1.0f)),
            Data3 = borderRadius.GetValueOrDefault(0.0f)
        };
        
        quad.Opts.Y = textureId;
        
        return quad;
    }
    
    public static Quad Sdf(int textureId, Mat3 transform, Vec2<float> size, Vec4<float>? color = null,
        Vec4<float>? uv = null)
    {
        var quad = new Quad(transform, size)
        {
            Mode = RenderMode.Sdf,
            Data1 = color.GetValueOrDefault(1.0f),
            Data2 = uv.GetValueOrDefault(new Vec4<float>(0.0f,0.0f,1.0f,1.0f)),
        };
        
        quad.Opts.Y = textureId;
        
        return quad;
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