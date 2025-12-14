using JetBrains.Annotations;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Vulkan.Images;

namespace Rin.Framework.Graphics.Vulkan.Graph;

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