using JetBrains.Annotations;

namespace Rin.Framework.Graphics;

[NoReorder]
public record struct SamplerSpec
{
    public required ImageFilter Filter;
    public required ImageTiling Tiling;
}