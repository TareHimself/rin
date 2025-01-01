namespace rin.Framework.Graphics.Shaders.Rsl;

public struct RslCompilationContext(IShaderManager manager) : ICompilationContext
{
    public IShaderManager Manager { get; } = manager;
}