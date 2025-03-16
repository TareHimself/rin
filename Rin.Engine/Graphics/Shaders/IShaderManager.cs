namespace Rin.Engine.Graphics.Shaders;

public interface IShaderManager : IDisposable
{
    public Task Compile(IShader shader);
    public IGraphicsShader MakeGraphics(string path);
    public IComputeShader MakeCompute(string path);
}