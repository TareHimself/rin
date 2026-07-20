using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     Resolves a <see cref="ResourceHandle" /> to the right external-resource descriptor for the render graph,
///     shared between <see cref="GraphBuilder" /> and <see cref="GraphConfig" /> so both stay in sync.
/// </summary>
internal static class ExternalResourceDescriptors
{
    public static IResourceDescriptor Make(in ResourceHandle handle, Action? onDispose)
    {
        return handle.Type switch
        {
            ResourceType.Texture => new ExternalVulkanTextureResourceDescriptor(
                VulkanGraphicsModule.Get().GetTexture(handle) ??
                throw new ArgumentException("Invalid texture resource handle", nameof(handle)),
                onDispose),
            ResourceType.Cubemap => new ExternalVulkanCubemapResourceDescriptor(
                VulkanGraphicsModule.Get().GetCubemap(handle) ??
                throw new ArgumentException("Invalid cubemap resource handle", nameof(handle)),
                onDispose),
            ResourceType.TextureArray => new ExternalVulkanTextureArrayResourceDescriptor(
                VulkanGraphicsModule.Get().GetTextureArray(handle) ??
                throw new ArgumentException("Invalid texture array resource handle", nameof(handle)),
                onDispose),
            _ => throw new ArgumentOutOfRangeException(nameof(handle), handle.Type,
                "Handle does not reference an image resource")
        };
    }
}
