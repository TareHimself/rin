using Rin.Framework.Graphics.Graph;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public class TextureArrayResourceDescriptor : IResourceDescriptor
{
    public readonly Extent2D Extent;
    public readonly ImageFormat Format;
    public readonly ImageUsage Usage;
    public readonly uint Count;

    public TextureArrayResourceDescriptor(in Extent2D extent, ImageFormat format, ImageUsage usage)
    {
        Extent = extent;
        Format = format;
        Usage = usage;
        Count = 1;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Extent, (int)Format, (int)Usage,Count);
    }
}