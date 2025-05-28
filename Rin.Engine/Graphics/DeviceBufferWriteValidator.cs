using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     A utility class used to ensure a device buffer has each section written only once
/// </summary>
public class DeviceBufferWriteValidator(IDeviceBuffer buffer) : IDeviceBuffer
{
    private readonly List<Pair<ulong, ulong>> _writes = [];
    //private HashSet<string> _writes = [];

    public void Dispose()
    {
        buffer.Dispose();
    }

    public IDeviceBuffer Buffer => this;
    public ulong Offset => buffer.Offset;
    public ulong Size => buffer.Size;
    public VkBuffer NativeBuffer => buffer.NativeBuffer;

    public ulong GetAddress()
    {
        return buffer.GetAddress();
    }

    public IDeviceBufferView GetView(ulong offset, ulong size)
    {
        return new DeviceBufferView(this, offset, size);
    }

    public unsafe void Write(void* src, ulong size, ulong offset = 0)
    {
        var write = new Pair<ulong, ulong>(offset, offset + size);
        foreach (var (begin, end) in _writes)
        {
            if ((write.First <= begin && write.Second <= begin) ||
                (write.First >= end && write.Second >= end)) continue;
            throw new Exception("Attempted buffer overwrite");
        }

        _writes.Add(write);
        buffer.Write(src, size, offset);
    }
}