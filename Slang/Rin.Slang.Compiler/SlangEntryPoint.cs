namespace Rin.Slang.Compiler;

public class SlangEntryPoint : IDisposable
{
    private readonly unsafe void* _ptr;

    public unsafe SlangEntryPoint(void* ptr)
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

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.slangEntryPointFree(_ptr);
        }
    }

    ~SlangEntryPoint()
    {
        ReleaseUnmanagedResources();
    }
}
