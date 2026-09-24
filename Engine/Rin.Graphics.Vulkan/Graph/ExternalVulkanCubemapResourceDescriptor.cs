using JetBrains.Annotations;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Graph;

public class ExternalVulkanCubemapResourceDescriptor : IExternalResourceDescriptor
{
    [PublicAPI] public readonly IDisposableVulkanCubemap Resource;

    IDisposable IExternalResourceDescriptor.Resource => Resource;

    public ExternalVulkanCubemapResourceDescriptor(IVulkanCubemap image, Action? onDispose = null)
    {
        Resource = new ExternalVulkanCubemap(image, onDispose);
    }

    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }
}