using System.Numerics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     Maths for a panorama of the whole sphere laid out as longitude by latitude: +Z at the centre, +X to its right
///     and +Y along the top row.
/// </summary>
public static class Panorama
{
    public static Vector3 Direction(float u, float v)
    {
        var polar = v * MathF.PI;
        var azimuth = (u - 0.5f) * MathF.Tau;
        var horizontal = MathF.Sin(polar);
        return new Vector3(horizontal * MathF.Sin(azimuth), MathF.Cos(polar), horizontal * MathF.Cos(azimuth));
    }

    public static int ReductionFactor(int width, int maxWidth)
    {
        return int.Max(1, (width + maxWidth - 1) / maxWidth);
    }

    /// <summary>
    ///     Shrinks an image by averaging <paramref name="factor" /> x <paramref name="factor" /> blocks. Columns and rows
    ///     left over at the right and bottom edges are dropped.
    /// </summary>
    public static void Reduce(ReadOnlySpan<Vector4> source, int width, int height, int factor,
        Span<Vector4> destination)
    {
        var reducedWidth = width / factor;
        var reducedHeight = height / factor;
        if (destination.Length != reducedWidth * reducedHeight)
            throw new ArgumentException("Destination must hold the reduced image.", nameof(destination));

        var blockWeight = 1f / (factor * factor);
        for (var y = 0; y < reducedHeight; y++)
        for (var x = 0; x < reducedWidth; x++)
        {
            var sum = Vector4.Zero;
            for (var blockY = 0; blockY < factor; blockY++)
            {
                var row = source.Slice((y * factor + blockY) * width + x * factor, factor);
                foreach (var texel in row) sum += texel;
            }

            destination[y * reducedWidth + x] = sum * blockWeight;
        }
    }

    public static SphericalHarmonics9 ProjectIrradiance(ReadOnlySpan<Vector4> texels, int width, int height)
    {
        var radiance = new SphericalHarmonics9();
        var texelAngles = MathF.Tau / width * (MathF.PI / height);
        for (var y = 0; y < height; y++)
        {
            var v = (y + 0.5f) / height;
            // Rows near the poles cover less of the sphere.
            var solidAngle = texelAngles * MathF.Sin(v * MathF.PI);
            for (var x = 0; x < width; x++)
            {
                var texel = texels[y * width + x];
                radiance.Add(Direction((x + 0.5f) / width, v), new Vector3(texel.X, texel.Y, texel.Z), solidAngle);
            }
        }

        return radiance.ConvolvedWithCosineLobe();
    }
}
