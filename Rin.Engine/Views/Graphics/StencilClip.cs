using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Graphics;

public struct StencilClip(
    Vector2 size,
    Mat3 transform)
{
    public Mat3 Transform = transform;
    public Vector2 Size = size;
}