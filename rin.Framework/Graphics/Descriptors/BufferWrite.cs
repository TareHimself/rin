namespace rin.Framework.Graphics.Descriptors;

public class BufferWrite
{
    public readonly IDeviceBuffer Buffer;
    public readonly ulong Offset;
    public readonly ulong Size;
    public readonly BufferType Type;


    /// <summary>
    ///     Construct a buffer write using a buffer
    /// </summary>
    /// <param name="buffer">The Buffer</param>
    /// <param name="type">The buffer type</param>
    /// <param name="offset">The offset of the write from the start of the buffer</param>
    /// <param name="size">The size of the write from the offset</param>
    public BufferWrite(IDeviceBuffer buffer, BufferType type, ulong? offset = null, ulong? size = null)
    {
        Buffer = buffer;
        Type = type;
        Offset = offset.GetValueOrDefault();
        Size = size.GetValueOrDefault(buffer.Size);
    }

    /// <summary>
    ///     Construct a buffer write using a view
    /// </summary>
    /// <param name="view">The view</param>
    /// <param name="type">The buffer type</param>
    /// ///
    /// <param name="offset">The offset of the write relative to the offset of the view</param>
    /// <param name="size">The size of the write from the offset</param>
    public BufferWrite(IDeviceBufferView view, BufferType type, ulong? offset = null, ulong? size = null)
    {
        Buffer = view.DeviceBuffer;
        Type = type;
        Offset = view.Offset + offset.GetValueOrDefault(0);
        Size = size.GetValueOrDefault(view.Size);
    }
}