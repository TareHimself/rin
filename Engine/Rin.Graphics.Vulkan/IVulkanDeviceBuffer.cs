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

    /// <summary>
    ///     The last render graph action on this buffer, carried between graphs so the next graph can synchronize
    ///     with it. Null until a graph has used the buffer.
    /// </summary>
    public BufferGraphState? GraphState { get; set; }

    public DeviceBufferView GetView()
    {
        return GetView(0, Size);
    }

    public ulong GetAddress();

    public DeviceBufferView GetView(ulong offset, ulong size);

    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0);
}
