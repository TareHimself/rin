using System.Numerics;
using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics.Quads;


public partial struct Quad(Mat3 transform, Vector2 size) : ICloneable<Quad>
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
    public Vector2 Size = size; 
    public Mat3 Transform = transform;
    public Vector4 Data1 = new Vector4(0.0f);
    public Vector4 Data2 = new Vector4(0.0f);
    public Vector4 Data3 = new Vector4(0.0f);
    public Vector4 Data4 = new Vector4(0.0f);

    public static Quad Rect(Mat3 transform, Vector2 size,Vector4? color = null,Vector4? borderRadius = null)
    {
        return new Quad(transform, size)
        {
            Mode = RenderMode.Shape,
            Data1 = color.GetValueOrDefault(new Vector4(1.0f)),
            Data2 = borderRadius.GetValueOrDefault(new Vector4())
        };
    }
    
    public static Quad Texture(int textureId,Mat3 transform, Vector2 size,Vector4? tint = null,Vector4? borderRadius = null,Vector4? uv = null)
    {
        var quad = new Quad(transform, size)
        {
            Mode = RenderMode.Texture,
            Data1 = tint.GetValueOrDefault(new Vector4(1.0f)),
            Data2 = uv.GetValueOrDefault(new Vector4(0.0f,0.0f,1.0f,1.0f)),
            Data3 = borderRadius.GetValueOrDefault(new Vector4())
        };
        
        quad.Opts.Y = textureId;
        
        return quad;
    }
    
    public static Quad Sdf(int textureId, Mat3 transform, Vector2 size, Vector4? color = null,
        Vector4? uv = null)
    {
        var quad = new Quad(transform, size)
        {
            Mode = RenderMode.Sdf,
            Data1 = color.GetValueOrDefault(new Vector4(1.0f)),
            Data2 = uv.GetValueOrDefault(new Vector4(0.0f,0.0f,1.0f,1.0f)),
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