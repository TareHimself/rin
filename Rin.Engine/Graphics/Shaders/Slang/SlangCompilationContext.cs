namespace Rin.Engine.Graphics.Shaders.Slang;

public readonly struct SlangCompilationContext(SlangShaderManager manager) : ICompilationContext
{
    public IShaderManager Manager { get; } = manager;
}