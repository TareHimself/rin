using System.Runtime.InteropServices;
using rin.Core.Math;

namespace rin.Widgets.Graphics.Quads;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Quad(
    
    Vector2<float> size,
    Matrix3 transform,
    int textureId = -1,
    int mode = 0)
{
    public int TextureId
    {
        get => Opts.X;
        set => Opts.X = value;
    }
    
    public int Mode
    {
        get => Opts.Y;
        set => Opts.Y = value;
    }
    
    public Vector4<int> Opts = new Vector4<int>(textureId,mode,0,0);
    public Vector4<float> Color = new Vector4<float>(1.0f);
    public Vector4<float> BorderRadius = new Vector4<float>(0.0f);
    public Vector2<float> Size = size; 
    public Matrix3 Transform = transform;
    public Vector4<float> UV = new Vector4<float>(0.0f,0.0f,1.0f,1.0f);
}