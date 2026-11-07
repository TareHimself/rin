using Rin.Core.Graphics;
using JetBrains.Annotations;

namespace Rin.Graphics.Vulkan;

[NoReorder]
public record struct SamplerSpec
{
    public required ImageFilter Filter;
    public required ImageTiling Tiling;
}