namespace rin.Framework.Graphics.Shaders;

public interface IShaderManager : IDisposable
{
    public event Action? OnBeforeDispose;
    public Task<CompiledShader> Compile(IShader shader);
    public IGraphicsShader GraphicsFromPath(string path);
    public IComputeShader ComputeFromPath(string path);
}