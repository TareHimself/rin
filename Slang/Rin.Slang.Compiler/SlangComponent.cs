namespace Rin.Slang.Compiler;

public class SlangComponent : IDisposable
{
    private readonly unsafe void* _ptr;

    public unsafe SlangComponent(void* ptr)
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

    public SlangBlob? GetEntryPointCode(int entryPointIndex, int targetIndex)
    {
        unsafe
        {
            var blob = Native.GetEntryPointCode(_ptr, entryPointIndex, targetIndex, null);
            if (blob == null) return null;
            return new SlangBlob(blob);
        }
    }

    public SlangBlob? GetEntryPointCode(int entryPointIndex, int targetIndex, ref SlangBlob diagnostics)
    {
        unsafe
        {
            var blob = Native.GetEntryPointCode(_ptr, entryPointIndex, targetIndex, diagnostics.ToPointer());
            if (blob == null) return null;
            return new SlangBlob(blob);
        }
    }

    public SlangBlob? GetEntryPointCode(int entryPointIndex, int targetIndex, SlangBlob outDiagnostics)
    {
        unsafe
        {
            var blob = Native.GetEntryPointCode(_ptr, entryPointIndex, targetIndex, outDiagnostics.ToPointer());
            return blob == null ? null : new SlangBlob(blob);
        }
    }

    public SlangSharedLibrary? GetEntryPointHostCallable(int entryPointIndex, int targetIndex,
        SlangBlob outDiagnostics)
    {
        unsafe
        {
            var library = Native.GetEntryPointHostCallable(_ptr, entryPointIndex, targetIndex,
                outDiagnostics.ToPointer());
            return library == null ? null : new SlangSharedLibrary(library);
        }
    }

    public SlangComponent? Link()
    {
        unsafe
        {
            var linked = Native.Link(_ptr, null);
            return linked == null ? null : new SlangComponent(linked);
        }
    }

    public SlangComponent? Link(SlangBlob outDiagnostics)
    {
        unsafe
        {
            var linked = Native.Link(_ptr, outDiagnostics.ToPointer());
            return linked == null ? null : new SlangComponent(linked);
        }
    }

    public SlangBlob ToLayoutJson()
    {
        unsafe
        {
            return new SlangBlob(Native.ToLayoutJson(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.slangComponentFree(_ptr);
        }
    }

    ~SlangComponent()
    {
        ReleaseUnmanagedResources();
    }
}
