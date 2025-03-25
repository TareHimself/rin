using System.Runtime.CompilerServices;
using Rin.Engine.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Rin.Engine.Views;

public static class ImageSharpExtensions
{
    public static byte[] ToBytes<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        var pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
        image.CopyPixelDataTo(pixelBytes);
        return pixelBytes;
    }

    public static Buffer<byte> ToBuffer<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        var nativeBuffer = new Buffer<byte>(image.Width * image.Height * Unsafe.SizeOf<Rgba32>());
        unsafe
        {
            image.CopyPixelDataTo(new Span<byte>(nativeBuffer.GetData(), (int)nativeBuffer.GetByteSize()));
        }

        return nativeBuffer;
    }
}