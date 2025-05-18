namespace Rin.Engine.Graphics.FrameGraph;

public class ImageResourceDescriptor : IResourceDescriptor
{
    public readonly Extent3D Extent;
    public readonly ImageFormat Format;
    public readonly ImageUsage Usage;

    public ImageResourceDescriptor(in Extent3D extent, ImageFormat format, ImageUsage usage)
    {
        Extent = extent;
        Format = format;
        Usage = usage;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Extent, (int)Format, (int)Usage);
    }
}