using Rin.Core.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Images;

public class VulkanCubemap : IDisposableVulkanCubemap
{
    public void Dispose()
    {
        VulkanGraphicsModule.Get().FreeImage(this);
    }

    public required VkImage VulkanImage { get; set; }
    public required VkImageView VulkanView { get; set; }
    public ImageLayout Layout { get; set; }
    public required IntPtr Allocation { get; set; }
    public required Extent2D Extent { get; set; }
    public const uint FaceCount = 6;

    public uint MipLevels { get; init; } = 1;
    public uint LayerCount => FaceCount;
    public required ImageFormat Format { get; set; }
    public ResourceHandle Handle { get; internal set; } = ResourceHandle.InvalidCubemap;
}