namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangSession : IDisposable
{
    private readonly unsafe void * _ptr;

    public unsafe SlangSession(void * ptr)
    {
        _ptr = ptr;
    }
    
    public unsafe void* ToPointer() => _ptr;
    // SlangResult Compile(string moduleName, string modulePath, string data, string entry)
    // {
    //     unsafe
    //     {
    //         return new SlangResult(NativeMethods.SlangSessionCompile(_ptr, moduleName, modulePath, data, entry));
    //     }
    // }

    public SlangModule LoadModuleFromSourceString(string moduleName, string path, string content)
    {
        unsafe
        {
            return new SlangModule(
                NativeMethods.SlangSessionLoadModuleFromSourceString(_ptr, moduleName, path, content, null));
        }
    }
    
    public SlangModule LoadModuleFromSourceString(string moduleName, string path, string content, ref SlangBlob outDiagnostics)
    {
        unsafe
        {
            return new SlangModule(NativeMethods.SlangSessionLoadModuleFromSourceString(_ptr, moduleName, path, content,outDiagnostics.ToPointer()));
        }
    }
    
    public SlangComponent? CreateComposedProgram(ref SlangSession module, ref SlangEntryPoint entryPoint)
    {
        unsafe
        {
            var ptr = NativeMethods.SlangSessionCreateComposedProgram(_ptr,module.ToPointer(),entryPoint.ToPointer(),null);
            return ptr != null ? new SlangComponent(ptr) : null;
        }
    }
    
    public SlangComponent? CreateComposedProgram(ref SlangSession module, ref SlangEntryPoint entryPoint, ref SlangBlob outDiagnostics)
    {
        unsafe
        {
            var ptr = NativeMethods.SlangSessionCreateComposedProgram(_ptr,module.ToPointer(),entryPoint.ToPointer(),outDiagnostics.ToPointer());
            return ptr != null ? new SlangComponent(ptr) : null;
        }
    }
    
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangSessionFree(_ptr);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangSession()
    {
        ReleaseUnmanagedResources();
    }
}