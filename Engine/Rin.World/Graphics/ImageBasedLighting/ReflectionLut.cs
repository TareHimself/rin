using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     The table that turns a surface's head-on reflectivity into how much of the environment it mirrors. Each texel
///     holds a scale and a bias, with the horizontal axis the cosine of the viewing angle and the vertical axis the
///     roughness, so that <c>reflectivity = baseReflectivity * scale + bias</c>.
/// </summary>
public static class ReflectionLut
{
    public const int Size = 32;

    private const int SampleCount = 512;

    public static void Generate(Span<Vector2> texels)
    {
        if (texels.Length != Size * Size)
            throw new ArgumentException("Destination must hold Size * Size texels.", nameof(texels));

        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
            texels[y * Size + x] = Integrate((y + 0.5f) / Size, (x + 0.5f) / Size);
    }

    /// <summary>
    ///     The scale and bias for one roughness and viewing angle, where <paramref name="facing" /> is the cosine of the
    ///     angle between the surface normal and the direction to the eye.
    /// </summary>
    public static Vector2 Integrate(float roughness, float facing)
    {
        var alpha = roughness * roughness;
        var view = new Vector3(MathF.Sqrt(1f - facing * facing), 0f, facing);
        var scale = 0f;
        var bias = 0f;
        for (var i = 0u; i < SampleCount; i++)
        {
            var half = GgxSampling.HalfVector((i + 0.5f) / SampleCount, GgxSampling.RadicalInverse(i), alpha);
            var viewDotHalf = Vector3.Dot(view, half);
            var lightFacing = 2f * viewDotHalf * half.Z - view.Z;
            if (lightFacing <= 0f || viewDotHalf <= 0f) continue;

            var visibility = Shadowing(lightFacing, alpha) * Shadowing(facing, alpha) * viewDotHalf / (half.Z * facing);
            var grazing = MathF.Pow(1f - viewDotHalf, 5f);
            scale += visibility * (1f - grazing);
            bias += visibility * grazing;
        }

        return new Vector2(scale, bias) / SampleCount;
    }

    public static ResourceHandle CreateTexture(IGraphicsModule graphics)
    {
        var extent = new Extent2D(Size, Size);
        var texture = graphics.CreateTexture(extent, ImageFormat.RG16,
            flags: ImageCreateFlags.Sampled | ImageCreateFlags.TransferDst);

        var texels = ArrayPool<Vector2>.Shared.Rent(Size * Size);
        var halves = ArrayPool<Half>.Shared.Rent(Size * Size * 2);
        try
        {
            var values = texels.AsSpan(0, Size * Size);
            var packed = halves.AsSpan(0, Size * Size * 2);
            Generate(values);
            for (var i = 0; i < values.Length; i++)
            {
                packed[i * 2] = (Half)values[i].X;
                packed[i * 2 + 1] = (Half)values[i].Y;
            }

            graphics.QueueImageUpload(texture, MemoryMarshal.AsBytes(packed), ImageRegion.Whole(extent));
        }
        finally
        {
            ArrayPool<Vector2>.Shared.Return(texels);
            ArrayPool<Half>.Shared.Return(halves);
        }

        return texture;
    }

    // How much of the surface's microscopic roughness is unblocked along one direction, the same term the light
    // loop uses.
    private static float Shadowing(float facing, float alpha)
    {
        var k = alpha / 2f;
        return facing / (facing * (1f - k) + k);
    }
}
