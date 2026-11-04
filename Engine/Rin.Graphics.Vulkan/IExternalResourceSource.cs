using Rin.Core.Graphics;
using Rin.Graphics.Vulkan.Images;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     Resolves and reference-counts the resources a graph imports by handle.
/// </summary>
internal interface IExternalResourceSource : IResourceAcquirer
{
    void ReleaseResource(in ResourceHandle handle);
    IVulkanTexture? GetTexture(in ResourceHandle handle);
    IVulkanCubemap? GetCubemap(in ResourceHandle handle);
    IVulkanTextureArray? GetTextureArray(in ResourceHandle handle);
    IVulkanDeviceBuffer? ResolveBuffer(in ResourceHandle handle);
}
