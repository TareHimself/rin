using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public class DeviceBufferView : IDeviceBuffer, IGraphResource
{
    private readonly IDeviceBuffer _buffer;
    private readonly int _inOffset;
    private readonly int _inSize;

    public DeviceBufferView(IDeviceBuffer buffer, int inOffset, int inSize)
    {
        _buffer = buffer;
        _inOffset = inOffset;
        _inSize = inSize;
        _buffer.Reserve();
    }

    public int Offset => _inOffset;
    public int Size => _inSize;
    public VkBuffer NativeBuffer => _buffer.NativeBuffer;
    public ulong GetAddress() => _buffer.GetAddress() + (ulong)Offset;

    public IDeviceBuffer GetView(int offset, int size)
    {
        return _buffer.GetView(Offset + offset,size);
    }

    public unsafe void Write(void* src, int size, int offset = 0)
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