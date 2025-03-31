using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Graphics;

public struct StencilClip(
    Vector2 size,
    Matrix4x4 transform)
{
    public Matrix4x4 Transform = transform;
    public Vector2 Size = size;
}