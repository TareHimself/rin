using System.Numerics;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.Passes.Blur;

public static class BlurPassExtensions
{
    public static CommandList AddBlur(this CommandList self,in Matrix4x4 transform,in Vector2 size, float strength = 5.0f,
        float radius = 5.0f, Vector4? tint = null)
    {
        self.Add(new ReadBackCommand());
        self.Add(new BlurCommand(transform, size, strength, radius, tint.GetValueOrDefault(Vector4.One)));
        return self;
    }
}