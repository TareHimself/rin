using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Graphics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct Clip(
    
    Vector2<float> size,
    Matrix3 transform)
{
    public Vector2<float> Size = size; 
    public Matrix3 Transform = transform;
}