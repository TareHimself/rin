using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     Resolves a <see cref="ResourceHandle" /> to the right external-resource descriptor for the render graph,
///     shared between <see cref="GraphBuilder" /> and <see cref="GraphConfig" /> so both stay in sync. Each
///     descriptor holds a reference on its resource, released when the graph disposes it.
/// </summary>
internal static class ExternalResourceDescriptors
{
    public static IResourceDescriptor? Make(in ResourceHandle handle, Action? onDispose)
    {
        var module = VulkanGraphicsModule.Get();
        if (!module.TryAcquireResource(handle)) return null;
        var release = MakeRelease(module, handle, onDispose);

        return handle.Type switch
        {
            ResourceType.Texture => new ExternalVulkanTextureResourceDescriptor(module.GetTexture(handle)!, release),
            ResourceType.Cubemap => new ExternalVulkanCubemapResourceDescriptor(module.GetCubemap(handle)!, release),
            ResourceType.TextureArray => new ExternalVulkanTextureArrayResourceDescriptor(
                module.GetTextureArray(handle)!, release),
            _ => throw new ArgumentOutOfRangeException(nameof(handle), handle.Type,
                "Handle does not reference an image resource")
        };
    }

    public static IResourceDescriptor? MakeBuffer(in DeviceBufferView view, Action? onDispose)
    {
        var module = VulkanGraphicsModule.Get();
        if (!module.TryAcquireResource(view.Buffer)) return null;

        return new ExternalVulkanBufferResourceDescriptor(module.ResolveBuffer(view.Buffer)!, view,
            MakeRelease(module, view.Buffer, onDispose));
    }

    private static Action MakeRelease(VulkanGraphicsModule module, ResourceHandle handle, Action? onDispose)
    {
        return () =>
        {
            module.ReleaseResource(handle);
            onDispose?.Invoke();
        };
    }
}
