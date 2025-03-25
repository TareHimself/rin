using System.Runtime.InteropServices;

namespace Rin.Engine.Graphics.Shaders.Slang;

public class SlangBlob : IDisposable
{
    private readonly unsafe void* _ptr;

    public unsafe SlangBlob()
    {
        _ptr = Native.Slang.BlobNew();
    }

    public unsafe SlangBlob(void* ptr)
    {
        _ptr = ptr;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public unsafe void* ToPointer()
    {
        return _ptr;
    }

    public int GetSize()
    {
        unsafe
        {
            return Native.Slang.BlobGetSize(_ptr);
        }
    }

    public IntPtr GetDataPointer()
    {
        unsafe
        {
            return new IntPtr(Native.Slang.BlobGetPointer(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.BlobFree(_ptr);
        }
    }

    public string GetString()
    {
        unsafe
        {
            return Marshal.PtrToStringAnsi(GetDataPointer()) ?? throw new NullReferenceException();
        }
    }

    ~SlangBlob()
    {
        ReleaseUnmanagedResources();
    }
}