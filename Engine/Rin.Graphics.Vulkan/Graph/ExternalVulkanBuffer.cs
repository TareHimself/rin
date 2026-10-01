using Rin.Core.Graphics;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Graph;

public class ExternalVulkanBuffer(IVulkanDeviceBuffer source, in DeviceBufferView view, Action? onDispose = null)
    : IVulkanDeviceBuffer
{
    public ulong Offset { get; } = view.Offset;
    public ulong Size { get; } = view.Size;
    public ResourceHandle Handle => source.Handle;
    public VkBuffer NativeBuffer => source.NativeBuffer;
    public IntPtr Allocation => source.Allocation;

    public BufferGraphState? GraphState
    {
        get => source.GraphState;
        set => source.GraphState = value;
    }

    public ulong GetAddress()
    {
        return source.GetAddress();
    }

    public DeviceBufferView GetView(ulong offset, ulong size)
    {
        return new DeviceBufferView(Handle, Offset + offset, size);
    }

    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0)
    {
        source.WriteRaw(src, size, Offset + offset);
    }

    public void Dispose()
    {
        onDispose?.Invoke();
    }
}
