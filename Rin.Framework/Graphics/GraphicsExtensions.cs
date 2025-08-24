using JetBrains.Annotations;

namespace Rin.Framework.Graphics;

public static class GraphicsExtensions
{
    [PublicAPI]
    public static ImageFormat ToDeviceFormat(this HostImageFormat hostFormat)
    {
        return hostFormat switch
        {
            HostImageFormat.R8 => ImageFormat.R8,
            HostImageFormat.R16 => ImageFormat.R16,
            HostImageFormat.R32 => ImageFormat.R32,
            HostImageFormat.RG8 => ImageFormat.RG8,
            HostImageFormat.RG16 => ImageFormat.RG16,
            HostImageFormat.RG32 => ImageFormat.RG32,
            HostImageFormat.RGBA8 => ImageFormat.RGBA8,
            HostImageFormat.RGBA16 => ImageFormat.RGBA16,
            HostImageFormat.RGBA32 => ImageFormat.RGBA32,
            _ => throw new ArgumentOutOfRangeException(nameof(hostFormat), hostFormat, null)
        };
    }
}