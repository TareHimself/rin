namespace rin.Graphics.Descriptors;

public class BufferWrite(DeviceBuffer.View view, BufferType type, ulong size, ulong offset)
{
    public readonly DeviceBuffer.View View = view;
    public readonly BufferType Type = type;
    public readonly ulong Offset = offset;
    public readonly ulong Size = size;

    public BufferWrite(DeviceBuffer.View view, BufferType type) : this(view,type,view.Size,0)
    {
        
    }
    
    public BufferWrite(DeviceBuffer.View view, BufferType type,ulong size) : this(view,type,size,0)
    {
        
    }
}