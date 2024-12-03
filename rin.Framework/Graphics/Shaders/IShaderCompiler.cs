namespace rin.Framework.Graphics.Shaders;

public interface IShaderCompiler : IDisposable
{
    public event Action? OnBeforeDispose;
    public Task<CompiledShader> Compile(IShader shader);
}