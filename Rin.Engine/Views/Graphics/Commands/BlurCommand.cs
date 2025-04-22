using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics.Passes;
using Rin.Engine.Views.Graphics.Passes.Blur;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Views.Graphics.Commands;



public class BlurCommand(in Matrix4x4 transform,in Vector2 size, float strength, float radius,in Vector4 tint) : TCommand<BlurPass>
{
    public Matrix4x4 Transform = transform;
    public Vector2 Size = size;
    public readonly float Strength = strength;
    public readonly float Radius = radius;
    public Vector4 Tint = tint;
}