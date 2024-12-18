namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangSessionBuilder : IDisposable
{

    private readonly unsafe void * _builder;

    public SlangSessionBuilder()
    {
        unsafe
        {
            _builder = NativeMethods.SlangSessionBuilderCreate();
        }
    }

    public SlangSessionBuilder AddTargetSpirv()
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddTargetSpirv(_builder);
        }
        return this;
    }
    
    public SlangSessionBuilder AddTargetGlsl()
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddTargetGlsl(_builder);
        }
        return this;
    }
    
    public SlangSessionBuilder AddPreprocessorDefinition(string name,string value)
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddPreprocessorDefinition(_builder,name,value);
        }
        return this;
    }
    
    public SlangSessionBuilder AddSearchPath(string searchPath)
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderAddSearchPath(_builder,searchPath);
        }
        return this;
    }

    public SlangSession Build()
    {
        unsafe
        {
            return new SlangSession(_builder);
        }
    }
    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            NativeMethods.SlangSessionBuilderDestroy(_builder);
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