using System.Runtime.InteropServices;

namespace rin.Sdf;

public class ByteBuffer : IDisposable
{
    public IntPtr Data { get; private set; }
    public int Size { get; private set; }

    public ByteBuffer(IntPtr data, int size)
    {
        Data = data;
        Size = size;
    }

    public static ByteBuffer CopyFrom(IntPtr src,int size)
    {
        var result = Marshal.AllocHGlobal(size);
        unsafe
        {
            Buffer.MemoryCopy(src.ToPointer(),result.ToPointer(),size,size);
        }

        return new ByteBuffer(result, size);
    }

    public static implicit operator Span<byte>(ByteBuffer buff)
    {
        unsafe
        {
            return new Span<byte>(buff.Data.ToPointer(), buff.Size);
        }
    }
    
    private void ReleaseUnmanagedResources()
    {
        Marshal.FreeHGlobal(Data);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~ByteBuffer()
    {
        ReleaseUnmanagedResources();
    }
}