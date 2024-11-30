using rin.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Graphics;

public class DeviceBufferView(IDeviceBuffer buffer, ulong inOffset, ulong inSize) : IDeviceBuffer, IGraphResource
{
    public ulong Offset => inOffset;
    public ulong Size => inSize;
    public VkBuffer NativeBuffer => buffer.NativeBuffer;
    public ulong GetAddress() => buffer.GetAddress() + Offset;

    public IDeviceBuffer GetView(ulong offset, ulong size)
    {
        return buffer.GetView(Offset + offset,size);
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        buffer.Write(src,size,Offset + offset);
    }


    public void Dispose()
    {
        buffer.Dispose();
    }

    public void Reserve()
    {
        buffer.Reserve();
    }
}