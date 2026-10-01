namespace Rin.Slang.Compiler;

/// <summary>A compiled host-callable (CPU) entry point, as a library its functions can be looked up in.</summary>
public sealed class SlangSharedLibrary : IDisposable
{
    private unsafe void* _ptr;

    internal unsafe SlangSharedLibrary(void* ptr)
    {
        _ptr = ptr;
    }

    public void Dispose()
    {
        unsafe
        {
            if (_ptr != null) Native.slangSharedLibraryFree(_ptr);
            _ptr = null;
        }

        GC.SuppressFinalize(this);
    }

    public IntPtr FindFunction(string name)
    {
        unsafe
        {
            return (IntPtr)Native.slangSharedLibraryFindFunc(_ptr, name);
        }
    }

    ~SlangSharedLibrary()
    {
        Dispose();
    }
}
