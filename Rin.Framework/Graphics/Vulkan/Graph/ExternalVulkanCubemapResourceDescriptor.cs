using JetBrains.Annotations;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Vulkan.Images;

namespace Rin.Framework.Graphics.Vulkan.Graph;

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