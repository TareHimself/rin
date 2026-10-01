using Rin.Shade;

namespace Rin.Core.Graphics.Shaders;

public interface IGraphicsShader : IShader
{
    public ImageFormat[] AttachmentFormats { get; }
    public BlendState BlendState { get; }
    public bool UsesStencil { get; }
    public bool UsesDepth { get; }

    public IGraphicsBindContext? Bind(IExecutionContext ctx, bool wait = true);
}