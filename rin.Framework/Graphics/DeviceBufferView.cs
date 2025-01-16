using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public class DeviceBufferView : IDeviceBuffer, IGraphResource
{
    private readonly IDeviceBuffer _buffer;
    private readonly ulong _inOffset;
    private readonly ulong _inSize;

    public DeviceBufferView(IDeviceBuffer buffer, ulong inOffset, ulong inSize)
    {
        _buffer = buffer;
        _inOffset = inOffset;
        _inSize = inSize;
        _buffer.Reserve();
    }

    public ulong Offset => _inOffset;
    public ulong Size => _inSize;
    public VkBuffer NativeBuffer => _buffer.NativeBuffer;
    public ulong GetAddress() => _buffer.GetAddress() + Offset;

    public IDeviceBuffer GetView(ulong offset, ulong size)
    {
        return _buffer.GetView(Offset + offset,size);
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        _buffer.Write(src,size,Offset + offset);
    }


    public void Dispose()
    {
        _buffer.Dispose();
    }

    public void Reserve()
    {
        _buffer.Reserve();
    }
}