namespace rin.Graphics.Descriptors;

public class BufferWrite(DeviceBuffer buffer, BufferType type, ulong size, ulong offset)
{
    public readonly DeviceBuffer Buffer = buffer;
    public readonly BufferType Type = type;
    public readonly ulong Offset = offset;
    public readonly ulong Size = size;

    public BufferWrite(DeviceBuffer buffer, BufferType type) : this(buffer,type,buffer.Size,0)
    {
        
    }
    
    public BufferWrite(DeviceBuffer buffer, BufferType type,ulong size) : this(buffer,type,size,0)
    {
        
    }
}