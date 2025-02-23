namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangModule : IDisposable
{
    private readonly unsafe void* _ptr;

    public unsafe SlangModule(void* ptr)
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

    public SlangEntryPoint? FindEntryPointByName(string name)
    {
        unsafe
        {
            var ptr = Native.Slang.SlangModuleFindEntryPointByName(_ptr, name);
            return ptr != null ? new SlangEntryPoint(ptr) : null;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.SlangModuleFree(_ptr);
        }
    }

    ~SlangModule()
    {
        ReleaseUnmanagedResources();
    }
}