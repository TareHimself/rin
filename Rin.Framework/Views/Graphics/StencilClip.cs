using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Framework.Views.Graphics;

[NoReorder]
public readonly struct StencilClip(
    in Matrix4x4 transform,
    in Vector2 size)
{
    public readonly Matrix4x4 Transform = transform;
    public readonly Vector2 Size = size;
}