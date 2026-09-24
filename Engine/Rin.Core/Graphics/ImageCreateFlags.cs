namespace Rin.Core.Graphics;

[Flags]
public enum ImageCreateFlags
{
    None = 0,
    TransferSrc = 1 << 0,
    TransferDst = 1 << 1,
    Sampled = 1 << 2,
    Storage = 1 << 3,
    ColorAttachment = 1 << 4,
    StencilAttachment = 1 << 5,
    DepthAttachment = 1 << 6,
}