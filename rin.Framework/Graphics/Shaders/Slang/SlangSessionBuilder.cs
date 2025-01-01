namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangSessionBuilder : IDisposable
{

    private readonly unsafe void * _ptr;

    public SlangSessionBuilder()
    {
        unsafe
        {
            _ptr = NativeMethods.SlangSessionBuilderNew();
        }
    }

    public SlangSessionBuilder AddTargetSpirv()
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddTargetSpirv(_ptr);
        }
        return this;
    }
    
    public SlangSessionBuilder AddTargetGlsl()
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddTargetGlsl(_ptr);
        }
        return this;
    }
    
    public SlangSessionBuilder AddPreprocessorDefinition(string name,string value)
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddPreprocessorDefinition(_ptr,name,value);
        }
        return this;
    }
    
    public SlangSessionBuilder AddSearchPath(string searchPath)
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddSearchPath(_ptr,searchPath);
        }
        return this;
    }

    public SlangSession Build()
    {
        unsafe
        {
            return new SlangSession(_ptr);
        }
    }
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderFree(_ptr);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~SlangSessionBuilder()
    {
        ReleaseUnmanagedResources();
    }
}