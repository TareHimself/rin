namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangModule : IDisposable
{
    private readonly unsafe void * _ptr;

    public unsafe SlangModule(void* ptr)
    {
        _ptr = ptr;
    }
    
    public unsafe void* ToPointer() => _ptr;
    
    public SlangEntryPoint? FindEntryPointByName(ref SlangSession module,string name)
    {
        unsafe
        {
            var ptr = NativeMethods.SlangModuleFindEntryPointByName(_ptr,name);
            return ptr != null ? new SlangEntryPoint(ptr) : null;
        }
    }
    
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangModuleFree(_ptr);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangModule()
    {
        ReleaseUnmanagedResources();
    }
}