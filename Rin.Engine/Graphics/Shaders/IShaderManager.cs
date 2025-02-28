namespace Rin.Engine.Graphics.Shaders;

public interface IShaderManager : IDisposable
{
    public event Action? OnBeforeDispose;
    public Task Compile(IShader shader);
    public IGraphicsShader GraphicsFromPath(string path);
    public IComputeShader ComputeFromPath(string path);
}