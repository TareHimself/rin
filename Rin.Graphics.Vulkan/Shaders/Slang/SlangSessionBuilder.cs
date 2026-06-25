namespace Rin.Graphics.Vulkan.Shaders.Slang;

public class SlangSessionBuilder : IDisposable
{
    private readonly unsafe void* _ptr;

    public SlangSessionBuilder()
    {
        unsafe
        {
            _ptr = Native.slangSessionBuilderNew();
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
            Native.slangSessionBuilderAddTargetSpirv(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddTargetGlsl()
    {
        unsafe
        {
            Native.slangSessionBuilderAddTargetGlsl(_ptr);
        }

        return this;
    }

    public SlangSessionBuilder AddPreprocessorDefinition(string name, string value)
    {
        unsafe
        {
            Native.slangSessionBuilderAddPreprocessorDefinition(_ptr, name, value);
        }

        return this;
    }

    public SlangSessionBuilder AddSearchPath(string searchPath)
    {
        unsafe
        {
            Native.slangSessionBuilderAddSearchPath(_ptr, searchPath);
        }

        return this;
    }

    public SlangSession Build()
    {
        unsafe
        {
            return new SlangSession(Native.slangSessionBuilderBuild(_ptr));
        }
    }

    private void ReleaseUnmanagedResources()
    {
        unsafe
        {
            Native.slangSessionBuilderFree(_ptr);
        }
    }

    ~SlangSessionBuilder()
    {
        ReleaseUnmanagedResources();
    }
}