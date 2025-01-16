namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangComponent : IDisposable
{
    private readonly unsafe void * _ptr;

    public unsafe SlangComponent(void* ptr)
    {
        _ptr = ptr;
    }
    
    public unsafe void* ToPointer() => _ptr;
    
    public SlangBlob? GetEntryPointCode(int entryPointIndex,int targetIndex)
    {
        unsafe
        {
            var blob = NativeMethods.SlangComponentGetEntryPointCode(_ptr, entryPointIndex, targetIndex, null);
            if (blob == null)
            {
                return null;
            }
            return new SlangBlob(blob);
        }
    }
    
    public SlangBlob? GetEntryPointCode(int entryPointIndex,int targetIndex, ref SlangBlob diagnostics)
    {
        unsafe
        {
            var blob = NativeMethods.SlangComponentGetEntryPointCode(_ptr, entryPointIndex, targetIndex, diagnostics.ToPointer());
            if (blob == null)
            {
                return null;
            }
            return new SlangBlob(blob);
        }
    }
    
    public SlangComponent? GetEntryPointCode(int entryPointIndex,int targetIndex,SlangBlob outDiagnostics)
    {
        unsafe
        {
            return new SlangComponent(NativeMethods.SlangComponentGetEntryPointCode(_ptr, entryPointIndex,targetIndex,outDiagnostics.ToPointer()));
        }
    }
    
    public SlangComponent? Link()
    {
        unsafe
        {
            return new SlangComponent(NativeMethods.SlangComponentLink(_ptr, null));
        }
    }
    
    public SlangComponent? Link(SlangBlob outDiagnostics)
    {
        unsafe
        {
            return new SlangComponent(NativeMethods.SlangComponentLink(_ptr, outDiagnostics.ToPointer()));
        }
    }
    
    public SlangBlob ToLayoutJson()
    {
        unsafe
        {
            return new SlangBlob(NativeMethods.SlangComponentToLayoutJson(_ptr));
        }
    }
    
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangComponentFree(_ptr);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangComponent()
    {
        ReleaseUnmanagedResources();
    }
}