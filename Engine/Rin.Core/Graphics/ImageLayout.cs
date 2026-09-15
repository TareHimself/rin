namespace Rin.Core.Graphics;

public enum ImageLayout
{
    Undefined,
    ColorAttachment,
    StencilAttachment,
    DepthAttachment,
    ShaderReadOnly,
    ShaderAccess,
    TransferSrc,
    TransferDst,
    Present
}