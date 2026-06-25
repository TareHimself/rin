using Rin.Core.Graphics.Shaders;

namespace Rin.Graphics.Vulkan.Shaders.Slang;

public readonly struct SlangCompilationContext(SlangShaderManager manager) : ICompilationContext
{
    public IShaderManager Manager { get; } = manager;
}