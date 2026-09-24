using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

public class TextureArrayResourceDescriptor : IResourceDescriptor
{
    public readonly uint Count;
    public readonly Extent2D Extent;
    public readonly ImageFormat Format;
    public readonly 
        ImageCreateFlags Usage;

    public TextureArrayResourceDescriptor(in Extent2D extent, ImageFormat format, ImageCreateFlags usage, uint count = 1)
    {
        Extent = extent;
        Format = format;
        Usage = usage;
        Count = count;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Extent, (int)Format, (int)Usage, Count);
    }
}