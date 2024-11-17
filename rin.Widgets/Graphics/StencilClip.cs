using rin.Core.Math;

namespace rin.Widgets.Graphics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct StencilClip(
    
    Vector2<float> size,
    Matrix3 transform)
{
    public Matrix3 Transform = transform;
    public Vector2<float> Size = size; 
}