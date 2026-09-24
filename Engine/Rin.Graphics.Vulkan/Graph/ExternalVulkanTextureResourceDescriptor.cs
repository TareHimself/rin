using JetBrains.Annotations;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan.Graph;

public class ExternalVulkanTextureResourceDescriptor : IExternalResourceDescriptor
{
    [PublicAPI] public readonly IDisposableVulkanTexture Resource;

    IDisposable IExternalResourceDescriptor.Resource => Resource;

    public ExternalVulkanTextureResourceDescriptor(IVulkanTexture image, Action? onDispose = null)
    {
        Resource = new ExternalVulkanTexture(image, onDispose);
    }

    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }
}