using rin.Framework.Core;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public interface IDeviceBufferView
{
    public IDeviceBuffer DeviceBuffer { get; }

    public ulong Offset { get; }
    public ulong Size { get; }
    public VkBuffer NativeBuffer { get; }

    public ulong GetAddress()
    {
        return DeviceBuffer.GetAddress() + Offset;
    }

    public IDeviceBufferView GetView(ulong offset, ulong size);

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        DeviceBuffer.Write(src, size, Offset + offset);
    }

    public void Write<T>(IEnumerable<T> data, ulong offset = 0) where T : unmanaged
    {
        DeviceBuffer.Write(data, Offset + offset);
    }

    public void Write<T>(T src, ulong offset = 0) where T : unmanaged
    {
        DeviceBuffer.Write(src, Offset + offset);
    }

    public void Write<T>(NativeBuffer<T> src, ulong offset = 0) where T : unmanaged
    {
        DeviceBuffer.Write(src, Offset + offset);
    }
}