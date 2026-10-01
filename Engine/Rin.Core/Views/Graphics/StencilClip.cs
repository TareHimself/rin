using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Core.Views.Graphics;

/// <summary>
///     One clip region as the stencil shader reads it. The inverse is computed once here, on the CPU, instead
///     of once per fragment on the GPU.
/// </summary>
[NoReorder]
public readonly struct StencilClip(
    in Matrix4x4 transform,
    in Vector2 size)
{
    public readonly Matrix4x4 Transform = transform;
    public readonly Matrix4x4 InverseTransform = Invert(transform);
    public readonly Vector2 Size = size;

    private static Matrix4x4 Invert(in Matrix4x4 matrix)
    {
        Matrix4x4.Invert(matrix, out var inverse);
        return inverse;
    }
}
