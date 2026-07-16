namespace Rin.Graphics.Vulkan.Images;

public interface IVulkanTextureArray : IVulkanImage
{
    public uint Count { get; }
}