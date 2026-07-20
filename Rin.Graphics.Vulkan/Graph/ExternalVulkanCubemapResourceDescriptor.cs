using JetBrains.Annotations;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Graph;

public class ExternalVulkanCubemapResourceDescriptor : IResourceDescriptor
{
    [PublicAPI] public readonly IDisposableVulkanCubemap Resource;

    public ExternalVulkanCubemapResourceDescriptor(IVulkanCubemap image, Action? onDispose = null)
    {
        Resource = new ExternalVulkanCubemap(image, onDispose);
    }

    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }
}