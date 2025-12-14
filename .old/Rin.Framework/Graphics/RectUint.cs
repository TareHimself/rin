using JetBrains.Annotations;

namespace Rin.Framework.Graphics;

public record struct RectUint
{
    [PublicAPI] public Extent2D Extent = default;
    [PublicAPI] public Offset2D Offset = default;

    public RectUint()
    {
    }

    public RectUint(in Offset2D offset, in Extent2D extent)
    {
        Offset = offset;
        Extent = extent;
    }

    public void Deconstruct(out Offset2D offset, out Extent2D extent)
    {
        offset = Offset;
        extent = Extent;
    }
}