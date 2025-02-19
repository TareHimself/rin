using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics;

public struct StencilClip(
    Vector2 size,
    Mat3 transform)
{
    public Mat3 Transform = transform;
    public Vector2 Size = size;
}