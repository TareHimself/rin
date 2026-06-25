using Rin.Core.Graphics;
using Rin.Core.Graphics.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Images;

public interface IVulkanImage : IImage
{
    public VkImage VulkanImage { get; }
    public VkImageView VulkanView { get; }
    public ImageLayout Layout { get; set; }
    public IntPtr Allocation { get; }
}