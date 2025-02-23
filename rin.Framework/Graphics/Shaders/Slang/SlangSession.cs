namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangSession : IDisposable
{
    private readonly unsafe void* _ptr;

    public unsafe SlangSession(void* ptr)
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
    // SlangResult Compile(string moduleName, string modulePath, string data, string entry)
    // {
    //     unsafe
    //     {
    //         return new SlangResult(NativeMethods.SlangSessionCompile(_ptr, moduleName, modulePath, data, entry));
    //     }
    // }

    public SlangModule? LoadModuleFromSourceString(string moduleName, string path, string content)
    {
        unsafe
        {
            using var diag = new SlangBlob();
            var module = Native.Slang.SlangSessionLoadModuleFromSourceString(_ptr, moduleName, path, content, null);
            if (module == null) return null;
            return new SlangModule(module);
        }
    }

    public SlangModule? LoadModuleFromSourceString(string moduleName, string path, string content,
        SlangBlob outDiagnostics)
    {
        unsafe
        {
            var module =
                Native.Slang.SlangSessionLoadModuleFromSourceString(_ptr, moduleName, path, content,
                    outDiagnostics.ToPointer());
            if (module == null) return null;
            return new SlangModule(module);
        }
    }

    public SlangComponent? CreateComposedProgram(SlangModule module, IEnumerable<SlangEntryPoint> entryPoints)
    {
        unsafe
        {
            var asArray = entryPoints.ToArray();
            var pEntryPoints = stackalloc nuint[asArray.Length];
            for (var i = 0; i < asArray.Length; i++) pEntryPoints[i] = (nuint)asArray[i].ToPointer();
            var ptr = Native.Slang.SlangSessionCreateComposedProgram(_ptr, module.ToPointer(), pEntryPoints,
                asArray.Length, null);
            return ptr != null ? new SlangComponent(ptr) : null;
        }
    }

    public SlangComponent? CreateComposedProgram(SlangModule module, IEnumerable<SlangEntryPoint> entryPoints,
        SlangBlob outDiagnostics)
    {
        unsafe
        {
            var asArray = entryPoints.ToArray();
            var pEntryPoints = stackalloc nuint[asArray.Length];
            for (var i = 0; i < asArray.Length; i++) pEntryPoints[i] = (nuint)asArray[i].ToPointer();
            var ptr = Native.Slang.SlangSessionCreateComposedProgram(_ptr, module.ToPointer(), pEntryPoints,
                asArray.Length, outDiagnostics.ToPointer());
            return ptr != null ? new SlangComponent(ptr) : null;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.SlangSessionFree(_ptr);
        }
    }

    ~SlangSession()
    {
        ReleaseUnmanagedResources();
    }
}