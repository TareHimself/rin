using Rin.Core.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan;

public interface IVulkanDeviceBuffer : IDisposable
{
    public ulong Offset { get; }
    public ulong Size { get; }
    public ResourceHandle Handle { get; }
    public VkBuffer NativeBuffer { get; }
    public IntPtr Allocation { get; }

    public DeviceBufferView GetView()
    {
        return GetView(0, Size);
    }

    public ulong GetAddress();

    public DeviceBufferView GetView(ulong offset, ulong size);

    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0);
}
