using Rin.Core.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Images;

public interface IVulkanImage
{
    public Extent2D Extent { get; }
    public bool Mips { get; }
    public ImageFormat Format { get; }
    public ResourceHandle Handle { get; }
    public VkImage VulkanImage { get; }
    public VkImageView VulkanView { get; }
    public ImageLayout Layout { get; set; }
    public IntPtr Allocation { get; }
}