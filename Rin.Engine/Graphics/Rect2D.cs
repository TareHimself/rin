using JetBrains.Annotations;

namespace Rin.Engine.Graphics;

public record struct Rect2D
{
    [PublicAPI] public Extent2D Extent = default;
    [PublicAPI] public Offset2D Offset = default;

    public Rect2D()
    {
    }

    public Rect2D(in Offset2D offset, in Extent2D extent)
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