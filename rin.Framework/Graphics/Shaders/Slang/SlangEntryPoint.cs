namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangEntryPoint : IDisposable
{
    private readonly unsafe void * _ptr;

    public unsafe SlangEntryPoint(void* ptr)
    {
        _ptr = ptr;
    }
    
    public unsafe void* ToPointer() => _ptr;
    
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangEntryPointFree(_ptr);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangEntryPoint()
    {
        ReleaseUnmanagedResources();
    }
}