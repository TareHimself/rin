using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Vulkan;

public interface IVulkanDeviceBuffer : IDeviceBuffer
{
    public VkBuffer NativeBuffer { get; }
    public IntPtr Allocation { get; }
}