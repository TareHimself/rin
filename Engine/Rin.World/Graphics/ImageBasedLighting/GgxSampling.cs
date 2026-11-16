using System.Numerics;
using Rin.Shade;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     Draws directions from the GGX distribution, the standard shape of the highlight on a rough surface, so that an
///     average over a few hundred of them stands in for an integral over the hemisphere. Shared by the reflection
///     table built on the CPU and the environment blur run in a shader.
/// </summary>
[ShadeExport]
public static class GgxSampling
{
    private const float Pi = 3.14159265359f;
    private const float Tau = 6.28318530718f;

    /// <summary>
    ///     A half vector around +Z for two numbers in 0 to 1, where <paramref name="alpha" /> is roughness squared.
    /// </summary>
    public static Vector3 HalfVector(float u, float v, float alpha)
    {
        var cosTheta = Sqrt((1f - v) / (1f + (alpha * alpha - 1f) * v));
        var sinTheta = Sqrt(1f - cosTheta * cosTheta);
        var phi = Tau * u;
        return new Vector3(sinTheta * Cos(phi), sinTheta * Sin(phi), cosTheta);
    }

    /// <summary>
    ///     How densely <see cref="HalfVector" /> samples a half vector whose Z is <paramref name="normalDotHalf" />.
    /// </summary>
    public static float Density(float normalDotHalf, float alpha)
    {
        var alphaSquared = alpha * alpha;
        var spread = normalDotHalf * normalDotHalf * (alphaSquared - 1f) + 1f;
        return alphaSquared / (Pi * spread * spread);
    }

    public static float RadicalInverse(uint index)
    {
        var bits = (index << 16) | (index >> 16);
        bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
        bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
        bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
        bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
        return bits * 2.3283064365386963e-10f;
    }
}
