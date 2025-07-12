using System.Numerics;
using Rin.Engine.Views.Graphics.CommandHandlers;
using Rin.Engine.Views.Graphics.PassConfigs;

namespace Rin.Engine.Views.Graphics.Commands;


public class BlurCommand(in Matrix4x4 transform, in Vector2 size, float strength, float radius, in Vector4 tint)
    : TCommand<BlurPassConfig,BlurCommandHandler>
{
    public readonly float Radius = radius;
    public readonly float Strength = strength;
    public Vector2 Size = size;
    public Vector4 Tint = tint;
    public Matrix4x4 Transform = transform;
}

public static class BlurPassExtensions
{
    public static CommandList AddBlur(this CommandList self, in Matrix4x4 transform, in Vector2 size,
        float strength = 5.0f,
        float radius = 5.0f, Vector4? tint = null)
    {
        self.Add(new ReadBackCommand());
        self.Add(new BlurCommand(transform, size, strength, radius, tint.GetValueOrDefault(Vector4.One)));
        return self;
    }
}