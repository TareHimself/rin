using Rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

public interface IVulkanImage2D : IImage2D
{
    public VkImage NativeImage { get; }
    public VkImageView NativeView { get; }
}