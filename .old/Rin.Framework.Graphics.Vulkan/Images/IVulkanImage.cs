using Rin.Framework.Graphics.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Images;

public interface IVulkanImage : IImage
{
    public VkImage VulkanImage { get; }
    public VkImageView VulkanView { get; }
    public ImageLayout Layout { get; set; }
    public IntPtr Allocation { get; }
}