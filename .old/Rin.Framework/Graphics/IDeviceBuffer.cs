using Rin.Framework.Graphics.Graph;

namespace Rin.Framework.Graphics;

public interface IDeviceBuffer : IDisposable
{
    public ulong Offset { get; }
    public ulong Size { get; }

    public DeviceBufferView GetView()
    {
        return GetView(0, Size);
    }

    public ulong GetAddress();

    public DeviceBufferView GetView(ulong offset, ulong size);

    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0);
}