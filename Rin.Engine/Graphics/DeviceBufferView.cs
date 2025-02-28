using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public class DeviceBufferView(IDeviceBuffer buffer, ulong inOffset, ulong inSize) : IDeviceBufferView
{
    public IDeviceBuffer DeviceBuffer => buffer;
    public ulong Offset { get; } = inOffset;
    public ulong Size { get; } = inSize;

    public VkBuffer NativeBuffer => buffer.NativeBuffer;

    public ulong GetAddress()
    {
        return buffer.GetAddress() + Offset;
    }

    public IDeviceBufferView GetView(ulong offset, ulong size)
    {
        return buffer.GetView(Offset + offset, size);
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        buffer.Write(src, size, Offset + offset);
    }
}