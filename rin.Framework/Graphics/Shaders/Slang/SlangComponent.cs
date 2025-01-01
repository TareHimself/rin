namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangComponent : IDisposable
{
    private readonly unsafe void * _ptr;

    public unsafe SlangComponent(void* ptr)
    {
        _ptr = ptr;
    }
    
    public unsafe void* ToPointer() => _ptr;
    
    public SlangComponent? GetEntryPointCode(int entryPointIndex,int targetIndex)
    {
        unsafe
        {
            return new SlangComponent(NativeMethods.SlangComponentGetEntryPointCode(_ptr, entryPointIndex,targetIndex,null));
        }
    }
    
    public SlangComponent? GetEntryPointCode(int entryPointIndex,int targetIndex, ref SlangBlob outDiagnostics)
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
    
    public SlangComponent? Link(ref SlangBlob outDiagnostics)
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