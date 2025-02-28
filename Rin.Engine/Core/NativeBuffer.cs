using System.Runtime.InteropServices;

namespace Rin.Engine.Core;

public class NativeBuffer<T>(int elements) : IDisposable
    where T : unmanaged
{
    private readonly IntPtr _ptr = Marshal.AllocHGlobal((int)Utils.ByteSizeOf<T>(elements));

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public unsafe T* GetData()
    {
        return (T*)_ptr;
    }

    public IntPtr GetPtr()
    {
        return _ptr;
    }

    public int GetElementsCount()
    {
        return elements;
    }

    public int GetByteSize()
    {
        return elements * Marshal.SizeOf<T>();
    }

    public static implicit operator Span<T>(NativeBuffer<T> buff)
    {
        unsafe
        {
            return new Span<T>(buff.GetPtr().ToPointer(), buff.GetElementsCount());
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (_ptr != IntPtr.Zero) Marshal.FreeHGlobal(_ptr);
    }

    public unsafe void Write(IntPtr src, uint size)
    {
        Buffer.MemoryCopy(src.ToPointer(), GetPtr().ToPointer(), GetByteSize(), size);
    }

    ~NativeBuffer()
    {
        ReleaseUnmanagedResources();
    }
}