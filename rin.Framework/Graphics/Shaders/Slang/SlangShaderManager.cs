namespace rin.Framework.Graphics.Shaders.Slang;

public class SlangShaderManager : IShaderManager
{
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public event Action? OnBeforeDispose;
    public Task Compile(IShader shader)
    {
        throw new NotImplementedException();
    }

    public IGraphicsShader GraphicsFromPath(string path)
    {
        throw new NotImplementedException();
    }

    public IComputeShader ComputeFromPath(string path)
    {
        throw new NotImplementedException();
    }
}