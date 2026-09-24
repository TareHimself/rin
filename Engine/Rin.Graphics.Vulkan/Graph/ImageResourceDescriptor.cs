using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

public class TextureResourceDescriptor : IResourceDescriptor
{
    public readonly Extent2D Extent;
    public readonly ImageFormat Format;
    public readonly ImageCreateFlags Usage;

    public TextureResourceDescriptor(in Extent2D extent, ImageFormat format, ImageCreateFlags usage)
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