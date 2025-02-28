namespace Rin.Engine.Graphics;

public static class Extensions
{
    public static uint ComponentCount(this ImageFormat format)
    {
        return format switch
        {
            ImageFormat.R8 => 1,
            ImageFormat.R16 => 1,
            ImageFormat.R32 => 1,
            ImageFormat.RG8 => 2,
            ImageFormat.RG16 => 2,
            ImageFormat.RG32 => 2,
            ImageFormat.RGB8 => 3,
            ImageFormat.RGB16 => 3,
            ImageFormat.RGB32 => 3,
            ImageFormat.RGBA8 => 4,
            ImageFormat.RGBA16 => 4,
            ImageFormat.RGBA32 => 4,
            ImageFormat.Depth => 1,
            ImageFormat.Stencil => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static ulong PixelByteSize(this ImageFormat format)
    {
        return format.ComponentCount() * (ulong)(format switch
        {
            ImageFormat.R8 => 1,
            ImageFormat.R16 => 2,
            ImageFormat.R32 => 4,
            ImageFormat.RG8 => 1,
            ImageFormat.RG16 => 2,
            ImageFormat.RG32 => 4,
            ImageFormat.RGB8 => 1,
            ImageFormat.RGB16 => 2,
            ImageFormat.RGB32 => 4,
            ImageFormat.RGBA8 => 1,
            ImageFormat.RGBA16 => 2,
            ImageFormat.RGBA32 => 4,
            ImageFormat.Depth => 4,
            ImageFormat.Stencil => 4, // Kinda finiky
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        });
    }
}