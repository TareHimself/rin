using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Images;

/// <summary>
///     Registry entry for a buffer. Its slot id is also its index in the bindless buffer array.
/// </summary>
public class BindlessBuffer : BindlessResource
{
    public IVulkanDeviceBuffer? Source { get; set; }
    public bool HostVisible { get; set; }
}
