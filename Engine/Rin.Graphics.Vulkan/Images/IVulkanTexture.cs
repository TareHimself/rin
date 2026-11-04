using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Images;

public interface IVulkanTexture : IVulkanImage
{
    public ImageCreateFlags Usage { get; }
}