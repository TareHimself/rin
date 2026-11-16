using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     An environment ready for <see cref="EnvironmentMaps.Create" />: its panorama as half floats for the GPU to turn
///     into a roughness-blurred cubemap, and its diffuse light as spherical harmonics.
/// </summary>
public sealed class EnvironmentMap
{
    public const int Channels = 4;

    private const int SmallestMipSize = 8;
    private const int PanoramaTexelsPerFaceTexel = 2;
    private const int IrradianceWidth = 128;
    private const float LargestHalf = 65504f;

    private readonly Half[] _panorama;

    private EnvironmentMap(Half[] panorama, Extent2D panoramaExtent, int faceSize, SphericalHarmonics9 irradiance)
    {
        _panorama = panorama;
        PanoramaExtent = panoramaExtent;
        FaceSize = faceSize;
        Irradiance = irradiance;
    }

    /// <summary>
    ///     Four halves per texel, shrunk so that it is no sharper than the cubemap can show.
    /// </summary>
    public ReadOnlySpan<Half> Panorama => _panorama;

    public Extent2D PanoramaExtent { get; }

    public int FaceSize { get; }

    /// <summary>
    ///     How many mips the cubemap has: mip 0 is the environment itself and the last is fully rough.
    /// </summary>
    public int MipCount => BitOperations.Log2((uint)(FaceSize / SmallestMipSize)) + 1;

    /// <summary>
    ///     Evaluating this at a surface normal gives the irradiance there.
    /// </summary>
    public SphericalHarmonics9 Irradiance { get; }

    public static float MipRoughness(int mip, int mipCount)
    {
        return mip / (mipCount - 1f);
    }

    public static EnvironmentMap FromRadiance(ReadOnlySpan<byte> file, int faceSize = 512)
    {
        var extent = RadianceImage.ReadExtent(file);
        var floatCount = (int)(extent.Width * extent.Height * Channels);
        var rented = ArrayPool<float>.Shared.Rent(floatCount);
        try
        {
            var rgba = rented.AsSpan(0, floatCount);
            RadianceImage.Decode(file, rgba);
            return FromEquirect(rgba, extent, faceSize);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(rented);
        }
    }

    public static EnvironmentMap FromEquirect(ReadOnlySpan<float> rgba, Extent2D extent, int faceSize = 512)
    {
        if (!BitOperations.IsPow2(faceSize)) throw new ArgumentOutOfRangeException(nameof(faceSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(faceSize, SmallestMipSize * 2);
        if (rgba.Length != (long)extent.Width * extent.Height * Channels)
            throw new ArgumentException("Pixels must hold width * height * 4 floats.", nameof(rgba));

        var source = MemoryMarshal.Cast<float, Vector4>(rgba);
        var width = (int)extent.Width;
        var height = (int)extent.Height;

        // A cubemap face spans a quarter of the panorama's width.
        var factor = ImageBasedLighting.Panorama.ReductionFactor(width, faceSize * 4 * PanoramaTexelsPerFaceTexel);
        var reducedWidth = width / factor;
        var reducedHeight = height / factor;
        var rented = ArrayPool<Vector4>.Shared.Rent(reducedWidth * reducedHeight);
        try
        {
            var reduced = rented.AsSpan(0, reducedWidth * reducedHeight);
            ImageBasedLighting.Panorama.Reduce(source, width, height, factor, reduced);
            return new EnvironmentMap(ToHalves(reduced), new Extent2D((uint)reducedWidth, (uint)reducedHeight),
                faceSize, ProjectIrradiance(reduced, reducedWidth, reducedHeight));
        }
        finally
        {
            ArrayPool<Vector4>.Shared.Return(rented);
        }
    }

    // Irradiance only keeps the low frequencies, so a small image gives the same answer for far less work.
    private static SphericalHarmonics9 ProjectIrradiance(ReadOnlySpan<Vector4> texels, int width, int height)
    {
        var factor = ImageBasedLighting.Panorama.ReductionFactor(width, IrradianceWidth);
        var smallWidth = width / factor;
        var smallHeight = height / factor;
        var rented = ArrayPool<Vector4>.Shared.Rent(smallWidth * smallHeight);
        try
        {
            var small = rented.AsSpan(0, smallWidth * smallHeight);
            ImageBasedLighting.Panorama.Reduce(texels, width, height, factor, small);
            return ImageBasedLighting.Panorama.ProjectIrradiance(small, smallWidth, smallHeight);
        }
        finally
        {
            ArrayPool<Vector4>.Shared.Return(rented);
        }
    }

    private static Half[] ToHalves(ReadOnlySpan<Vector4> texels)
    {
        var floats = MemoryMarshal.Cast<Vector4, float>(texels);
        var halves = new Half[floats.Length];
        for (var i = 0; i < floats.Length; i++) halves[i] = (Half)MathF.Min(floats[i], LargestHalf);
        return halves;
    }
}
