using Rin.Core.Graphics.Shaders;

namespace Rin.Graphics.Vulkan.Shaders.Compiled;

public readonly struct CompiledShaderCompilationContext(CompiledShaderManager manager) : ICompilationContext
{
    public IShaderManager Manager { get; } = manager;
}
