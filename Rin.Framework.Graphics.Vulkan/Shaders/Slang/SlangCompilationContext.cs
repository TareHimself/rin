using Rin.Framework.Graphics.Shaders;

namespace Rin.Framework.Graphics.Vulkan.Shaders.Slang;

public readonly struct SlangCompilationContext(SlangShaderManager manager) : ICompilationContext
{
    public IShaderManager Manager { get; } = manager;
}