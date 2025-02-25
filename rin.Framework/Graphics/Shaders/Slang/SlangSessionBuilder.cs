namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangSessionBuilder : IDisposable
{
    private readonly unsafe void* _ptr;

    public SlangSessionBuilder()
    {
        unsafe
        {
            _ptr = Native.Slang.SessionBuilderNew();
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
            Native.Slang.SessionBuilderAddTargetSpirv(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddTargetGlsl()
    {
        unsafe
        {
            Native.Slang.SessionBuilderAddTargetGlsl(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddPreprocessorDefinition(string name, string value)
    {
        unsafe
        {
            Native.Slang.SessionBuilderAddPreprocessorDefinition(_ptr, name, value);
        }

        return this;
    }

    public SlangSessionBuilder AddSearchPath(string searchPath)
    {
        unsafe
        {
            Native.Slang.SessionBuilderAddSearchPath(_ptr, searchPath);
        }

        return this;
    }

    public SlangSession Build()
    {
        unsafe
        {
            return new SlangSession(Native.Slang.SessionBuilderBuild(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.Slang.SessionBuilderFree(_ptr);
        }
    }

    ~SlangSessionBuilder()
    {
        ReleaseUnmanagedResources();
    }
}