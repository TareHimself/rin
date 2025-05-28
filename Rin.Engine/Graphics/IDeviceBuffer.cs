using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public interface IDeviceBuffer : IDeviceBufferView, IGraphResource
{
    public IDeviceBufferView GetView()
    {
        return GetView(0, Size);
    }

    public new unsafe void Write(void* src, ulong size, ulong offset = 0);
    
}