namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangBlob : IDisposable
{
    private readonly unsafe void* _ptr;

    public unsafe SlangBlob()
    {
        _ptr = Native.Slang.SlangBlobNew();
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
            return Native.Slang.SlangBlobGetSize(_ptr);
        }
    }

    public IntPtr GetDataPointer()
    {
        unsafe
        {
            return new IntPtr(Native.Slang.SlangBlobGetPointer(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.SlangBlobFree(_ptr);
        }
    }

    ~SlangBlob()
    {
        ReleaseUnmanagedResources();
    }
}