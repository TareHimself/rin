using System.Buffers;
using System.Buffers.Text;

namespace Rin.Core.Graphics;

public static class RadianceImage
{
    private const int Channels = 4;
    private const int MinRunLengthWidth = 8;
    private const int MaxRunLengthWidth = 32767;
    private const int MaxLiteralLength = 128;
    private const int MantissaBits = 8;
    private const int ExponentBias = 128;

    private static readonly float[] ExponentScales = BuildExponentScales();

    public static bool IsRadiance(ReadOnlySpan<byte> data)
    {
        return data.StartsWith("#?RADIANCE"u8) || data.StartsWith("#?RGBE"u8);
    }

    public static Extent2D ReadExtent(ReadOnlySpan<byte> data)
    {
        return ReadHeader(data, out _, out _);
    }

    /// <summary>
    ///     Decodes into <paramref name="rgba" />, which must hold width * height * 4 floats. Alpha is 1. The header's
    ///     EXPOSURE is divided back out.
    /// </summary>
    public static void Decode(ReadOnlySpan<byte> data, Span<float> rgba)
    {
        var extent = ReadHeader(data, out var pixels, out var exposure);
        var gain = 1f / exposure;
        var width = (int)extent.Width;
        var rowLength = width * Channels;
        if (rgba.Length != (long)rowLength * extent.Height)
            throw new ArgumentException("Destination must hold width * height * 4 floats.", nameof(rgba));

        var rented = ArrayPool<byte>.Shared.Rent(rowLength);
        try
        {
            var planes = rented.AsSpan(0, rowLength);
            for (var y = 0; y < extent.Height; y++)
            {
                var row = rgba.Slice(y * rowLength, rowLength);
                pixels = IsRunLengthEncoded(pixels, width)
                    ? DecodeRunLengthScanline(pixels, planes, row, gain)
                    : DecodeFlatScanline(pixels, row, gain);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static float[] BuildExponentScales()
    {
        var scales = new float[256];
        // Exponent 0 means a black pixel, so its scale stays 0.
        for (var exponent = 1; exponent < scales.Length; exponent++)
            scales[exponent] = MathF.ScaleB(1f, exponent - ExponentBias - MantissaBits);
        return scales;
    }

    private static Extent2D ReadHeader(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> pixels, out float exposure)
    {
        if (!IsRadiance(data)) throw new InvalidDataException("Not a Radiance image.");

        var remaining = data;
        var isRgbe = false;
        exposure = 1f;
        for (var line = ReadLine(ref remaining); !line.IsEmpty; line = ReadLine(ref remaining))
        {
            isRgbe |= line.SequenceEqual("FORMAT=32-bit_rle_rgbe"u8);
            // Each tool that rescales the pixels adds its own line, so the factors multiply.
            if (line.StartsWith("EXPOSURE="u8)) exposure *= ParseExposure(line["EXPOSURE=".Length..]);
        }

        if (!isRgbe) throw new InvalidDataException("Only 32-bit_rle_rgbe Radiance images are supported.");

        var extent = ParseResolution(ReadLine(ref remaining));
        pixels = remaining;
        return extent;
    }

    private static ReadOnlySpan<byte> ReadLine(ref ReadOnlySpan<byte> remaining)
    {
        var length = remaining.IndexOf((byte)'\n');
        if (length < 0) throw new InvalidDataException("Radiance header is truncated.");

        var line = remaining[..length].TrimEnd((byte)'\r');
        remaining = remaining[(length + 1)..];
        return line;
    }

    private static float ParseExposure(ReadOnlySpan<byte> value)
    {
        if (!Utf8Parser.TryParse(value.Trim((byte)' '), out float exposure, out _) || !(exposure > 0f)
            || !float.IsFinite(exposure))
            throw new InvalidDataException("Radiance EXPOSURE must be a positive number.");
        return exposure;
    }

    private static Extent2D ParseResolution(ReadOnlySpan<byte> line)
    {
        const string unsupported = "Only top-down, left-to-right Radiance images (-Y height +X width) are supported.";
        if (!line.StartsWith("-Y "u8) || !Utf8Parser.TryParse(line[3..], out uint height, out var consumed))
            throw new InvalidDataException(unsupported);

        line = line[(3 + consumed)..];
        if (!line.StartsWith(" +X "u8) || !Utf8Parser.TryParse(line[4..], out uint width, out _))
            throw new InvalidDataException(unsupported);

        if (width == 0 || height == 0 || (ulong)width * height * Channels > int.MaxValue)
            throw new InvalidDataException("Radiance image size is out of range.");
        return new Extent2D(width, height);
    }

    private static bool IsRunLengthEncoded(ReadOnlySpan<byte> pixels, int width)
    {
        return width is >= MinRunLengthWidth and <= MaxRunLengthWidth
               && pixels.Length >= 4
               && pixels[0] == 2 && pixels[1] == 2
               && ((pixels[2] << 8) | pixels[3]) == width;
    }

    private static ReadOnlySpan<byte> DecodeRunLengthScanline(ReadOnlySpan<byte> pixels, Span<byte> planes,
        Span<float> row, float gain)
    {
        pixels = pixels[4..];
        var width = planes.Length / Channels;
        for (var channel = 0; channel < Channels; channel++)
            pixels = DecodeRunLengthPlane(pixels, planes.Slice(channel * width, width));

        var red = planes[..width];
        var green = planes.Slice(width, width);
        var blue = planes.Slice(width * 2, width);
        var exponent = planes.Slice(width * 3, width);
        for (var x = 0; x < width; x++)
            WritePixel(row.Slice(x * Channels, Channels), red[x], green[x], blue[x], exponent[x], gain);
        return pixels;
    }

    private static ReadOnlySpan<byte> DecodeRunLengthPlane(ReadOnlySpan<byte> pixels, Span<byte> plane)
    {
        while (!plane.IsEmpty)
        {
            if (pixels.Length < 2) throw Truncated();

            int count = pixels[0];
            var isRun = count > MaxLiteralLength;
            if (isRun) count -= MaxLiteralLength;
            if (count == 0 || count > plane.Length) throw new InvalidDataException("Radiance scanline is corrupt.");

            if (isRun)
            {
                plane[..count].Fill(pixels[1]);
                pixels = pixels[2..];
            }
            else
            {
                if (pixels.Length < 1 + count) throw Truncated();
                pixels.Slice(1, count).CopyTo(plane);
                pixels = pixels[(1 + count)..];
            }

            plane = plane[count..];
        }

        return pixels;
    }

    private static ReadOnlySpan<byte> DecodeFlatScanline(ReadOnlySpan<byte> pixels, Span<float> row, float gain)
    {
        if (pixels.Length < row.Length) throw Truncated();

        for (var i = 0; i < row.Length; i += Channels)
            WritePixel(row.Slice(i, Channels), pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3], gain);
        return pixels[row.Length..];
    }

    private static void WritePixel(Span<float> pixel, byte red, byte green, byte blue, byte exponent, float gain)
    {
        var scale = ExponentScales[exponent] * gain;
        pixel[3] = 1f;
        pixel[2] = blue * scale;
        pixel[1] = green * scale;
        pixel[0] = red * scale;
    }

    private static InvalidDataException Truncated()
    {
        return new InvalidDataException("Radiance pixel data is truncated.");
    }
}
