using JetBrains.Annotations;

namespace Rin.Core.Graphics;

[NoReorder]
public record struct SamplerSpec
{
    public required ImageFilter Filter;
    public required ImageTiling Tiling;
}