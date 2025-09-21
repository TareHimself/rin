using JetBrains.Annotations;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Vulkan.Images;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public class ExternalVulkanTextureResourceDescriptor : IResourceDescriptor
{
    [PublicAPI] public readonly IDisposableVulkanTexture Resource;
    
    public ExternalVulkanTextureResourceDescriptor(IVulkanTexture image, Action? onDispose = null)
    {
        Resource = new ExternalVulkanTexture(image, onDispose);
    }

    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }
}