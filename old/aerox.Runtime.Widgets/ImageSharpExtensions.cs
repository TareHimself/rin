using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace aerox.Runtime.Widgets;

public static class ImageSharpExtensions
{
    public static byte[] ToBytes<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
        image.CopyPixelDataTo(pixelBytes);
        return pixelBytes;
    }
}