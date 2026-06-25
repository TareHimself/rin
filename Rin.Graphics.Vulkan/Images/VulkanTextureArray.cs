using Rin.Core.Graphics;
using Rin.Core.Graphics.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Images;

public class VulkanTextureArray : IDisposableVulkanTextureArray
{
    public void Dispose()
    {
        VulkanGraphicsModule.Get().FreeImage(this);
    }

    public required VkImage VulkanImage { get; set; }
    public required VkImageView VulkanView { get; set; }
    public ImageLayout Layout { get; set; }
    public required Extent2D Extent { get; set; }
    public bool Mips { get; set; }
    public required ImageFormat Format { get; set; }
    public ImageHandle Handle { get; } = ImageHandle.InvalidTextureArray;
    public required uint Count { get; set; }

    public required IntPtr Allocation { get; set; }
}