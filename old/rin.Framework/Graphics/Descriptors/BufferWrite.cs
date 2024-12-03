namespace rin.Framework.Graphics.Descriptors;

public class BufferWrite(IDeviceBuffer view, BufferType type, ulong size, ulong offset)
{
    public readonly IDeviceBuffer View = view;
    public readonly BufferType Type = type;
    public readonly ulong Offset = offset;
    public readonly ulong Size = size;

    public BufferWrite(IDeviceBuffer view, BufferType type) : this(view,type,view.Size,0)
    {
        
    }
    
    public BufferWrite(IDeviceBuffer view, BufferType type,ulong size) : this(view,type,size,0)
    {
        
    }
}