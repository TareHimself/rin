using System.Numerics;
using Rin.Engine.Views.Graphics.Passes.Blur;

namespace Rin.Engine.Views.Graphics.Commands;

public class BlurCommand(in Matrix4x4 transform, in Vector2 size, float strength, float radius, in Vector4 tint)
    : TCommand<BlurPass>
{
    public readonly float Radius = radius;
    public readonly float Strength = strength;
    public Vector2 Size = size;
    public Vector4 Tint = tint;
    public Matrix4x4 Transform = transform;
}