using JetBrains.Annotations;
using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Graph;

public class ExternalVulkanBufferResourceDescriptor : IExternalResourceDescriptor
{
    [PublicAPI] public readonly IVulkanDeviceBuffer Resource;

    IDisposable IExternalResourceDescriptor.Resource => Resource;

    public ExternalVulkanBufferResourceDescriptor(IVulkanDeviceBuffer buffer, in DeviceBufferView view,
        Action? onDispose = null)
    {
        Resource = new ExternalVulkanBuffer(buffer, view, onDispose);
    }

    public override int GetHashCode()
    {
        return Resource.GetHashCode();
    }
}
