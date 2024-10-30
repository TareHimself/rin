using System.Runtime.InteropServices;

namespace rin.Core;

public class NativeBuffer<T>(int elements) : IDisposable
    where T : unmanaged
{
    private readonly IntPtr _ptr = Marshal.AllocHGlobal((Marshal.SizeOf<T>()) * elements);

    public unsafe T* GetData()
    {
        return (T*)(_ptr);
    }
    
    public unsafe IntPtr GetPtr()
    {
        return _ptr;
    }

    public int GetElements() => elements;
    
    public int GetByteSize() => elements * Marshal.SizeOf<T>();
    
    public static implicit operator Span<T>(NativeBuffer<T> buff)
    {
        unsafe
        {
            return new Span<T>(buff.GetPtr().ToPointer(), buff.GetElements());
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (_ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_ptr);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~NativeBuffer()
    {
        ReleaseUnmanagedResources();
    }
}