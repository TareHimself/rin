using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan;

public struct BufferBarrier(DeviceBufferView view, GraphBufferUsage from, GraphBufferUsage to, ResourceOperation fromOperation, ResourceOperation toOperation)
{
    public DeviceBufferView View = view;
    public GraphBufferUsage From = from;
    public GraphBufferUsage To = to;
    public ResourceOperation FromOperation = fromOperation;
    public ResourceOperation ToOperation = toOperation;
}
