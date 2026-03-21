using JetBrains.Annotations;

namespace Rin.Framework.Graphics;

[NoReorder]
public record struct ImageRect
{
    [PublicAPI] public Offset2D Offset = default;
    [PublicAPI] public Extent3D Extent = default;

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