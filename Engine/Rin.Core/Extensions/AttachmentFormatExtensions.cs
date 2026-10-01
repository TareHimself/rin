using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.Core.Extensions;

public static class AttachmentFormatExtensions
{
    public static ImageFormat ToImageFormat(this AttachmentFormat format)
    {
        return format switch
        {
            AttachmentFormat.R8 => ImageFormat.R8,
            AttachmentFormat.R16 => ImageFormat.R16,
            AttachmentFormat.R32 => ImageFormat.R32,
            AttachmentFormat.RG8 => ImageFormat.RG8,
            AttachmentFormat.RG16 => ImageFormat.RG16,
            AttachmentFormat.RG32 => ImageFormat.RG32,
            AttachmentFormat.RGBA8 => ImageFormat.RGBA8,
            AttachmentFormat.RGBA16 => ImageFormat.RGBA16,
            AttachmentFormat.RGBA32 => ImageFormat.RGBA32,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}
