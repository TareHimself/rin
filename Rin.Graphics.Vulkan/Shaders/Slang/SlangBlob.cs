using System.Runtime.InteropServices;

namespace Rin.Graphics.Vulkan.Shaders.Slang;

public class SlangBlob : IDisposable
{
    private readonly unsafe void* _ptr;

    public unsafe SlangBlob()
    {
        _ptr = Native.slangBlobNew();
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
            return Native.slangBlobGetSize(_ptr);
        }
    }

    public IntPtr GetDataPointer()
    {
        unsafe
        {
            return new IntPtr(Native.slangBlobGetPointer(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.slangBlobFree(_ptr);
        }
    }

    public string GetString()
    {
        return Marshal.PtrToStringAnsi(GetDataPointer()) ?? throw new NullReferenceException();
    }

    public ReadOnlySpan<byte> AsReadOnlySpan()
    {
        unsafe
        {
            return new ReadOnlySpan<byte>(GetDataPointer().ToPointer(), GetSize());
        }
    }

    ~SlangBlob()
    {
        ReleaseUnmanagedResources();
    }
}