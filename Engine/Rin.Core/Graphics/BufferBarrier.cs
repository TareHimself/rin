namespace Rin.Core.Graphics;

public struct BufferBarrier(DeviceBufferView view, BufferUsage from, BufferUsage to, ResourceOperation fromOperation, ResourceOperation toOperation)
{
    public DeviceBufferView View = view;
    public BufferUsage From = from;
    public BufferUsage To = to;
    public ResourceOperation FromOperation = fromOperation;
    public ResourceOperation ToOperation = toOperation;
}