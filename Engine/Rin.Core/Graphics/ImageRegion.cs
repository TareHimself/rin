namespace Rin.Core.Graphics;

/// <summary>
///     Where a write lands in an image: a rectangle of one mip level of one layer (an array slice or a cubemap face).
/// </summary>
public readonly record struct ImageRegion(Extent2D Extent, Offset2D Offset = default, uint Mip = 0, uint Layer = 0)
{
    public static ImageRegion Whole(Extent2D extent)
    {
        return new ImageRegion(extent);
    }

    public static ImageRegion Rect(Offset2D offset, Extent2D extent)
    {
        return new ImageRegion(extent, offset);
    }

    public static ImageRegion OfMip(Extent2D fullExtent, uint mip, uint layer = 0)
    {
        return new ImageRegion(fullExtent.MipExtent(mip), default, mip, layer);
    }

    public static ImageRegion OfFace(Extent2D fullExtent, CubeFace face, uint mip = 0)
    {
        return OfMip(fullExtent, mip, (uint)face);
    }
}
