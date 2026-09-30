using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;

namespace Rin.Graphics.Null;

// Bind always returning null short-circuits every pass's `if (Shader.Bind(ctx) is { } bindContext)` guard.
internal sealed class NullGraphicsShader : IGraphicsShader
{
    public bool Ready => true;
    public ImageFormat[] AttachmentFormats => [];
    public BlendState BlendState => BlendState.None;
    public bool UsesStencil => false;
    public bool UsesDepth => false;

    public void Compile(ICompilationContext context)
    {
    }

    public IGraphicsBindContext? Bind(IExecutionContext ctx, bool wait = true)
    {
        return null;
    }

    public void Dispose()
    {
    }
}
