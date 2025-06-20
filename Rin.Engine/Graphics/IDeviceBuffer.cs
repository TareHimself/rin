using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public interface IDeviceBuffer : IDeviceBufferWriteOps, IGraphResource
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
    
}