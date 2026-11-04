using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     Resolves a <see cref="ResourceHandle" /> to the right external-resource descriptor for the render graph,
///     shared between <see cref="GraphBuilder" /> and <see cref="GraphConfig" /> so both stay in sync. Each
///     descriptor holds a reference on its resource, released when the graph disposes it.
/// </summary>
internal static class ExternalResourceDescriptors
{
    internal static IResourceDescriptor? Make(IExternalResourceSource source, in ResourceHandle handle,
        Action? onDispose)
    {
        if (!source.TryAcquireResource(handle)) return null;
        var release = MakeRelease(source, handle, onDispose);

        return handle.Type switch
        {
            ResourceType.Texture => new ExternalVulkanTextureResourceDescriptor(source.GetTexture(handle)!, release),
            ResourceType.Cubemap => new ExternalVulkanCubemapResourceDescriptor(source.GetCubemap(handle)!, release),
            ResourceType.TextureArray => new ExternalVulkanTextureArrayResourceDescriptor(
                source.GetTextureArray(handle)!, release),
            _ => throw new ArgumentOutOfRangeException(nameof(handle), handle.Type,
                "Handle does not reference an image resource")
        };
    }

    internal static IResourceDescriptor? MakeBuffer(IExternalResourceSource source, in DeviceBufferView view,
        Action? onDispose)
    {
        if (!source.TryAcquireResource(view.Buffer)) return null;

        return new ExternalVulkanBufferResourceDescriptor(source.ResolveBuffer(view.Buffer)!, view,
            MakeRelease(source, view.Buffer, onDispose));
    }

    private static Action MakeRelease(IExternalResourceSource source, ResourceHandle handle, Action? onDispose)
    {
        return () =>
        {
            source.ReleaseResource(handle);
            onDispose?.Invoke();
        };
    }
}
