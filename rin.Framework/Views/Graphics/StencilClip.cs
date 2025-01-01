using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct StencilClip(
    
    Vec2<float> size,
    Mat3 transform)
{
    public Mat3 Transform = transform;
    public Vec2<float> Size = size; 
}