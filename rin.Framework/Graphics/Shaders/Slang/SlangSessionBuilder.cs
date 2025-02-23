namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangSessionBuilder : IDisposable
{
    private readonly unsafe void* _ptr;

    public SlangSessionBuilder()
    {
        unsafe
        {
            _ptr = Native.Slang.SlangSessionBuilderNew();
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public SlangSessionBuilder AddTargetSpirv()
    {
        unsafe
        {
            Native.Slang.SlangSessionBuilderAddTargetSpirv(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddTargetGlsl()
    {
        unsafe
        {
            Native.Slang.SlangSessionBuilderAddTargetGlsl(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddPreprocessorDefinition(string name, string value)
    {
        unsafe
        {
            Native.Slang.SlangSessionBuilderAddPreprocessorDefinition(_ptr, name, value);
        }

        return this;
    }

    public SlangSessionBuilder AddSearchPath(string searchPath)
    {
        unsafe
        {
            Native.Slang.SlangSessionBuilderAddSearchPath(_ptr, searchPath);
        }

        return this;
    }

    public SlangSession Build()
    {
        unsafe
        {
            return new SlangSession(Native.Slang.SlangSessionBuilderBuild(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.SlangSessionBuilderFree(_ptr);
        }
    }

    ~SlangSessionBuilder()
    {
        ReleaseUnmanagedResources();
    }
}