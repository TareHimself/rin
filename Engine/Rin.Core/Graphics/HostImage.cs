using System.Diagnostics;
using NetVips;
using Rin.Core.Shared.Buffers;
using VipsImage = NetVips.Image;
using VipsMutableImage = NetVips.MutableImage;

namespace Rin.Core.Graphics;

public class HostImage : IHostImage
{
    private static bool _CACHE_CHANGED = ChangeCache();
    private readonly VipsImage _image;

    private HostImage(VipsImage image)
    {
        // Take ownership of the source image and fully materialise it in memory now, so the
        // resulting HostImage never depends on the caller keeping a stream/buffer alive.
        using (image)
        {
            var rgba = image.Bands == 3 ? image.Bandjoin(255) : image;
            try
            {
                _image = rgba.CopyMemory();
            }
            finally
            {
                if (!ReferenceEquals(rgba, image))
                {
                    rgba.Dispose();
                }
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _image.Dispose();
    }

    public Task<ResourceHandle> CreateTexture(out ResourceHandle handle, ImageFilter filter = ImageFilter.Linear,
        ImageTiling tiling = ImageTiling.Repeat, bool mips = false, string? debugName = null,
        IGraphicsModule? graphicsModule = null)
    {
        return (graphicsModule ?? IGraphicsModule.Get()).CreateTexture(out handle, _image.RawsaveBuffer(), Extent,
            Format.ToDeviceFormat(), mips);
    }

    public Extent2D Extent => new()
    {
        Width = (uint)_image.Width,
        Height = (uint)_image.Height
    };

    public uint Channels => (uint)_image.Bands;

    public HostImageFormat Format
    {
        get
        {
            return _image.Bands switch
            {
                1 => _image.Format switch
                {
                    Enums.BandFormat.Uchar or Enums.BandFormat.Char => HostImageFormat.R8,
                    Enums.BandFormat.Ushort or Enums.BandFormat.Short => HostImageFormat.R16,
                    Enums.BandFormat.Uint or Enums.BandFormat.Int or Enums.BandFormat.Float => HostImageFormat.R32,
                    _ => throw new ArgumentOutOfRangeException()
                },
                2 => _image.Format switch
                {
                    Enums.BandFormat.Uchar or Enums.BandFormat.Char => HostImageFormat.RG8,
                    Enums.BandFormat.Ushort or Enums.BandFormat.Short => HostImageFormat.RG16,
                    Enums.BandFormat.Uint or Enums.BandFormat.Int or Enums.BandFormat.Float => HostImageFormat.RG32,
                    _ => throw new ArgumentOutOfRangeException()
                },
                4 => _image.Format switch
                {
                    Enums.BandFormat.Uchar or Enums.BandFormat.Char => HostImageFormat.RGBA8,
                    Enums.BandFormat.Ushort or Enums.BandFormat.Short => HostImageFormat.RGBA16,
                    Enums.BandFormat.Uint or Enums.BandFormat.Int or Enums.BandFormat.Float => HostImageFormat.RGBA32,
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    
    public void SaveRaw(Stream output)
    {
        _image.RawsaveStream(output);
    }

    public void SavePng(Stream output)
    {
        _image.PngsaveStream(output);
    }

    public IHostImage Mutate(Action<IMutationContext> mutator)
    {
        return new HostImage(_image.Mutate(image => mutator(new MutationContext(image))));
    }


    private static bool ChangeCache()
    {
        Cache.Max = 0;
        Cache.MaxMem = 0;
        return true;
    }

    public static IHostImage Create(in Extent2D extent, ImageFormat format)
    {
        //VipsImage.Black(width,height,).
        uint channels = 0;
        var bandFormat = Enums.BandFormat.Uchar;
        uint byteSize = 1;
        switch (format)
        {
            case ImageFormat.R8:
                channels = 1;
                byteSize = 1;
                bandFormat = Enums.BandFormat.Uchar;
                break;
            case ImageFormat.R16:
                channels = 1;
                byteSize = 2;
                bandFormat = Enums.BandFormat.Short;
                break;
            case ImageFormat.R32:
                channels = 1;
                byteSize = 4;
                bandFormat = Enums.BandFormat.Float;
                break;
            case ImageFormat.RG8:
                channels = 2;
                byteSize = 1;
                bandFormat = Enums.BandFormat.Uchar;
                break;
            case ImageFormat.RG16:
                channels = 2;
                byteSize = 2;
                bandFormat = Enums.BandFormat.Short;
                break;
            case ImageFormat.RG32:
                channels = 2;
                byteSize = 4;
                bandFormat = Enums.BandFormat.Float;
                break;
            case ImageFormat.RGBA8:
                channels = 4;
                byteSize = 1;
                bandFormat = Enums.BandFormat.Uchar;
                break;
            case ImageFormat.RGBA16:
                channels = 4;
                byteSize = 2;
                bandFormat = Enums.BandFormat.Short;
                break;
            case ImageFormat.RGBA32:
                channels = 4;
                byteSize = 4;
                bandFormat = Enums.BandFormat.Float;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        var size = extent.Width * extent.Height * channels * byteSize;
        using var buff = new Buffer<byte>((int)size);
        buff.Zero();
        return new HostImage(VipsImage.NewFromMemoryCopy(buff.GetPtr(), size, (int)extent.Width, (int)extent.Height,
            (int)channels, bandFormat));
    }

    /// <summary>
    ///     Decodes an encoded image (png, jpeg, ...) from <paramref name="data" />. The bytes are
    ///     copied and decoded before this returns, so the caller keeps ownership of the span.
    /// </summary>
    public static IHostImage Create(ReadOnlySpan<byte> data)
    {
        return new HostImage(VipsImage.NewFromBuffer(data.ToArray()));
    }

    /// <summary>
    ///     Reads <paramref name="data" /> to its end and decodes it as an encoded image. The stream
    ///     is fully consumed before this returns and may be disposed by the caller immediately after.
    /// </summary>
    public static IHostImage Create(Stream data)
    {
        using var buffer = new MemoryStream();
        data.CopyTo(buffer);
        return Create(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    }

    public static IHostImage Create(in ReadOnlyMemory<byte> data, uint width, uint height, uint channels)
    {
        return new HostImage(VipsImage.NewFromMemory(data, (int)width, (int)height, (int)channels,
            Enums.BandFormat.Char));
    }

    public static IHostImage Create(Buffer<byte> data, uint width, uint height, uint channels)
    {
        return new HostImage(VipsImage.NewFromMemoryCopy(data.GetPtr(), data.GetByteSize(), (int)width, (int)height,
            (int)channels, Enums.BandFormat.Uchar));
    }

    public static IHostImage Create(IntPtr ptr, uint width, uint height, uint channels)
    {
        return new HostImage(VipsImage.NewFromMemoryCopy(ptr, width * height * channels, (int)width, (int)height,
            (int)channels, Enums.BandFormat.Uchar));
    }

    private class MutationContext(VipsMutableImage mutableImage) : IMutationContext
    {
        public IMutationContext DrawImage(IHostImage image, in Offset2D offset)
        {
            Debug.Assert(image is HostImage);
            mutableImage.DrawImage(((HostImage)image)._image, (int)offset.X, (int)offset.Y, Enums.CombineMode.Set);
            return this;
        }

        public IMutationContext AddImage(IHostImage image, in Offset2D offset)
        {
            Debug.Assert(image is HostImage);
            mutableImage.DrawImage(((HostImage)image)._image, (int)offset.X, (int)offset.Y, Enums.CombineMode.Add);
            return this;
        }

        public IMutationContext DrawRect(in Offset2D offset, in Extent2D extent, params ReadOnlySpan<double> values)
        {
            mutableImage.DrawRect(values.ToArray(), (int)offset.X, (int)offset.Y, (int)extent.Width, (int)extent.Height, true);
            return this;
        }

        public IMutationContext Fill(
            params ReadOnlySpan<double> values)
        {
            return DrawRect(Offset2D.Zero,
                new Extent2D((uint)mutableImage.Width, (uint)mutableImage.Height), values);
        }
    }
}