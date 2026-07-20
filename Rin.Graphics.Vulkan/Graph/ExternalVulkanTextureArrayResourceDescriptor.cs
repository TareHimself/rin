using JetBrains.Annotations;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Graph;

public class ExternalVulkanTextureArrayResourceDescriptor : IResourceDescriptor
{
    [PublicAPI] public readonly IDisposableVulkanTextureArray Resource;

    public ExternalVulkanTextureArrayResourceDescriptor(IVulkanTextureArray image, Action? onDispose = null)
    {
        Resource = new ExternalVulkanTextureArray(image, onDispose);
    }

    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }
}