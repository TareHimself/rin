using Rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

public interface IDeviceBuffer :  IGraphResource
{
    public ulong Offset { get; }
    public ulong Size { get; }
    public VkBuffer NativeBuffer { get; }

    public DeviceBufferView GetView()
    {
        return GetView(0, Size);
    }

    public ulong GetAddress();

    public DeviceBufferView GetView(ulong offset, ulong size);

    public void WriteRaw(in IntPtr src, ulong size, ulong offset = 0);
}