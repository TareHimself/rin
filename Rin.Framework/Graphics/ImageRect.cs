using JetBrains.Annotations;

namespace Rin.Framework.Graphics;

public record struct ImageRect
{
    [PublicAPI] public Extent3D Extent = default;
    [PublicAPI] public Offset2D Offset = default;

    public ImageRect()
    {
    }

    public ImageRect(in Offset2D offset, in Extent3D extent)
    {
        Offset = offset;
        Extent = extent;
    }

    public void Deconstruct(out Offset2D offset, out Extent3D extent)
    {
        offset = Offset;
        extent = Extent;
    }
}