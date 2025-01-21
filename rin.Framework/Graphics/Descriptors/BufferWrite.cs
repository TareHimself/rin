namespace rin.Framework.Graphics.Descriptors;

public class BufferWrite(IDeviceBuffer view, BufferType type, int size,int offset)
{
    public readonly IDeviceBuffer View = view;
    public readonly BufferType Type = type;
    public readonly int Offset = offset;
    public readonly int Size = size;

    public BufferWrite(IDeviceBuffer view, BufferType type) : this(view,type,view.Size,0)
    {
        
    }
    
    public BufferWrite(IDeviceBuffer view, BufferType type,int size) : this(view,type,size,0)
    {
        
    }
}