

namespace Rin.Framework.Graphics;

public record struct SamplerSpec
{
    public required ImageFilter Filter;
    public required ImageTiling Tiling;
}