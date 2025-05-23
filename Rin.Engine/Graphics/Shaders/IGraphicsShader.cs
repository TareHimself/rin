namespace Rin.Engine.Graphics.Shaders;

public interface IGraphicsShader : IShader
{
    public ImageFormat[] AttachmentFormats { get; }
    public BlendMode BlendMode { get; }

    public bool UsesStencil { get; }
    public bool UsesDepth { get; }
}