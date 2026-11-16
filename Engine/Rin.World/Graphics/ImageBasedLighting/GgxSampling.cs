using System.Numerics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     Draws directions from the GGX distribution, the standard shape of the highlight on a rough surface, so that an
///     average over a few hundred of them stands in for an integral over the hemisphere.
/// </summary>
public static class GgxSampling
{
    /// <summary>
    ///     A half vector around +Z for two numbers in 0 to 1, where <paramref name="alpha" /> is roughness squared.
    /// </summary>
    public static Vector3 HalfVector(float u, float v, float alpha)
    {
        var cosTheta = MathF.Sqrt((1f - v) / (1f + (alpha * alpha - 1f) * v));
        var sinTheta = MathF.Sqrt(1f - cosTheta * cosTheta);
        var phi = MathF.Tau * u;
        return new Vector3(sinTheta * MathF.Cos(phi), sinTheta * MathF.Sin(phi), cosTheta);
    }

    public static float RadicalInverse(uint index)
    {
        index = (index << 16) | (index >> 16);
        index = ((index & 0x55555555u) << 1) | ((index & 0xAAAAAAAAu) >> 1);
        index = ((index & 0x33333333u) << 2) | ((index & 0xCCCCCCCCu) >> 2);
        index = ((index & 0x0F0F0F0Fu) << 4) | ((index & 0xF0F0F0F0u) >> 4);
        index = ((index & 0x00FF00FFu) << 8) | ((index & 0xFF00FF00u) >> 8);
        return index * (1f / 4294967296f);
    }
}
