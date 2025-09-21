using Rin.Framework.Graphics.Graph;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public class CubemapResourceDescriptor : IResourceDescriptor
{
    public readonly Extent2D Extent;
    public readonly ImageFormat Format;
    public readonly ImageUsage Usage;

    public CubemapResourceDescriptor(in Extent2D extent, ImageFormat format, ImageUsage usage)
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