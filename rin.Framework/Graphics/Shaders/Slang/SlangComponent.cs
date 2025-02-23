namespace rin.Framework.Graphics.Shaders.Slang;

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
            var blob = Native.Slang.SlangComponentGetEntryPointCode(_ptr, entryPointIndex, targetIndex, null);
            if (blob == null) return null;
            return new SlangBlob(blob);
        }
    }

    public SlangBlob? GetEntryPointCode(int entryPointIndex, int targetIndex, ref SlangBlob diagnostics)
    {
        unsafe
        {
            var blob = Native.Slang.SlangComponentGetEntryPointCode(_ptr, entryPointIndex, targetIndex,
                diagnostics.ToPointer());
            if (blob == null) return null;
            return new SlangBlob(blob);
        }
    }

    public SlangComponent? GetEntryPointCode(int entryPointIndex, int targetIndex, SlangBlob outDiagnostics)
    {
        unsafe
        {
            return new SlangComponent(Native.Slang.SlangComponentGetEntryPointCode(_ptr, entryPointIndex, targetIndex,
                outDiagnostics.ToPointer()));
        }
    }

    public SlangComponent? Link()
    {
        unsafe
        {
            return new SlangComponent(Native.Slang.SlangComponentLink(_ptr, null));
        }
    }

    public SlangComponent? Link(SlangBlob outDiagnostics)
    {
        unsafe
        {
            return new SlangComponent(Native.Slang.SlangComponentLink(_ptr, outDiagnostics.ToPointer()));
        }
    }

    public SlangBlob ToLayoutJson()
    {
        unsafe
        {
            return new SlangBlob(Native.Slang.SlangComponentToLayoutJson(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.SlangComponentFree(_ptr);
        }
    }

    ~SlangComponent()
    {
        ReleaseUnmanagedResources();
    }
}