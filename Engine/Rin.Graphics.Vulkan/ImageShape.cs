using Rin.Core.Graphics;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan;

internal readonly record struct ImageShape(Extent2D Extent, uint MipLevels = 1, uint LayerCount = 1)
{
    public ulong LevelTexelCount(uint level)
    {
        var levelExtent = Extent.MipExtent(level);
        return (ulong)levelExtent.Width * levelExtent.Height * LayerCount;
    }

    public ulong ChainTexelCount
    {
        get
        {
            ulong count = 0;
            for (uint level = 0; level < MipLevels; level++) count += LevelTexelCount(level);
            return count;
        }
    }
}

internal static class ImageShapeExtensions
{
    public static ImageShape Shape(this IVulkanImage image)
    {
        return new ImageShape(image.Extent, image.MipLevels, image.LayerCount);
    }
}
