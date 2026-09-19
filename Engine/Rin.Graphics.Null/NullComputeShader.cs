using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;

namespace Rin.Graphics.Null;

// Bind always returning null short-circuits every pass's `if (Shader.Bind(ctx) is { } bindContext)` guard.
internal sealed class NullComputeShader : IComputeShader
{
    public bool Ready => true;
    public uint GroupSizeX => 1;
    public uint GroupSizeY => 1;
    public uint GroupSizeZ => 1;

    public void Compile(ICompilationContext context)
    {
    }

    public IComputeBindContext? Bind(IExecutionContext ctx, bool wait = true)
    {
        return null;
    }

    public void Dispose()
    {
    }
}
