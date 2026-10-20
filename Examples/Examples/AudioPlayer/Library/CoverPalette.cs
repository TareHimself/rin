using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views;

namespace Examples.AudioPlayer.Library;

/// <summary>
///     Two vivid colors taken from a cover: the hue that covers most of it and the strongest hue far from that one.
///     A cover with no color (black and white art) gets <see cref="Default" />.
/// </summary>
public readonly record struct CoverPalette(Color Primary, Color Secondary)
{
    public static readonly CoverPalette Default = new(new Color(0.42f, 0.48f, 1f, 1f), new Color(0.62f, 0.85f, 1f, 1f));

    private const int HueBuckets = 24;
    private const int MinHueDistance = 4;
    private const int MaxSamples = 4096;
    private const float MinWeight = 0.02f;
    private const float SecondaryShare = 0.15f;
    private const float SecondaryHueShift = 0.11f;
    private const float MinSaturation = 0.55f;
    private const float MinValue = 0.8f;

    public static CoverPalette From(IHostImage image)
    {
        if (image.Format != HostImageFormat.RGBA8) return Default;

        using var stream = new MemoryStream();
        image.SaveRaw(stream);
        return From(stream.GetBuffer().AsSpan(0, (int)stream.Length), 4);
    }

    public static CoverPalette From(ReadOnlySpan<byte> pixels, int channels)
    {
        var count = pixels.Length / channels;
        if (count == 0) return Default;

        Span<float> weights = stackalloc float[HueBuckets];
        Span<Vector3> sums = stackalloc Vector3[HueBuckets];
        var step = int.Max(1, count / MaxSamples);
        for (var i = 0; i < count; i += step)
        {
            var offset = i * channels;
            var rgb = new Vector3(pixels[offset], pixels[offset + 1], pixels[offset + 2]) / 255f;
            var (hue, saturation, value) = ToHsv(rgb);
            var weight = saturation * value * value;
            if (weight < MinWeight) continue;

            var bucket = int.Min((int)(hue * HueBuckets), HueBuckets - 1);
            weights[bucket] += weight;
            sums[bucket] += rgb * weight;
        }

        var primaryBucket = 0;
        for (var bucket = 1; bucket < HueBuckets; bucket++)
            if (weights[bucket] > weights[primaryBucket])
                primaryBucket = bucket;
        if (weights[primaryBucket] <= 0f) return Default;

        var primary = Vivid(sums[primaryBucket] / weights[primaryBucket]);

        var secondaryBucket = -1;
        for (var bucket = 0; bucket < HueBuckets; bucket++)
        {
            if (HueDistance(bucket, primaryBucket) < MinHueDistance) continue;
            if (weights[bucket] < weights[primaryBucket] * SecondaryShare) continue;
            if (secondaryBucket < 0 || weights[bucket] > weights[secondaryBucket]) secondaryBucket = bucket;
        }

        var secondary = secondaryBucket >= 0
            ? Vivid(sums[secondaryBucket] / weights[secondaryBucket])
            : ShiftHue(primary, SecondaryHueShift);
        return new CoverPalette(primary, secondary);
    }

    private static int HueDistance(int a, int b)
    {
        var distance = int.Abs(a - b);
        return int.Min(distance, HueBuckets - distance);
    }

    private static Color Vivid(Vector3 rgb)
    {
        var (hue, saturation, value) = ToHsv(rgb);
        return Color.FromHsv(hue, float.Max(saturation, MinSaturation), float.Max(value, MinValue));
    }

    private static Color ShiftHue(Color color, float shift)
    {
        var (hue, saturation, value) = ToHsv(new Vector3(color.R, color.G, color.B));
        return Color.FromHsv((hue + shift) % 1f, saturation, value);
    }

    private static (float Hue, float Saturation, float Value) ToHsv(Vector3 rgb)
    {
        var max = float.Max(rgb.X, float.Max(rgb.Y, rgb.Z));
        var min = float.Min(rgb.X, float.Min(rgb.Y, rgb.Z));
        var delta = max - min;
        if (delta <= 0f || max <= 0f) return (0f, 0f, max);

        var hue = max == rgb.X ? (rgb.Y - rgb.Z) / delta % 6f
            : max == rgb.Y ? (rgb.Z - rgb.X) / delta + 2f
            : (rgb.X - rgb.Y) / delta + 4f;
        hue /= 6f;
        if (hue < 0f) hue += 1f;
        return (hue, delta / max, max);
    }
}
