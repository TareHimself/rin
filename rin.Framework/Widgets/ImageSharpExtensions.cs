using System.Runtime.CompilerServices;
using rin.Framework.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace rin.Framework.Widgets;

public static class ImageSharpExtensions
{
    public static byte[] ToBytes<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
        image.CopyPixelDataTo(pixelBytes);
        return pixelBytes;
    }
    
    public static NativeBuffer<byte> ToBuffer<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {
        var nativeBuffer = new NativeBuffer<byte>(image.Width * image.Height * Unsafe.SizeOf<Rgba32>());
        unsafe
        {
            image.CopyPixelDataTo(new Span<byte>(nativeBuffer.GetData(), nativeBuffer.GetByteSize()));
        }
        return nativeBuffer;
    }
}