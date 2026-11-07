using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.Default.Shaders;

/// <summary>
///     One view's light grid: the point lights sorted by view depth, the z-bins from <see cref="LightBinner" />, and one
///     bitmask word per 32 lights per screen tile.
/// </summary>
[NoReorder]
public struct LightGridInfo
{
    public BufferRef<LightInfo> PointLights;
    public BufferRef<LightInfo> DirectionalLights;
    public BufferRef<uint> Bins;
    public BufferRef<uint> TileMasks;
    public Matrix4x4 View;
    public int PointLightCount;
    public int DirectionalLightCount;
    public uint Width;
    public uint Height;
    public uint TilesX;
    public uint TilesY;
    public uint WordCount;
    public float Near;
    public float BinScale;
    public float TanHalfFovY;
    public float Aspect;
}

/// <summary>
///     Grid lookups shared by the tile pass and the shading loop. Everything but <see cref="BinOf" /> is plain
///     arithmetic, so it also runs as ordinary C# in tests.
/// </summary>
public static class LightGrid
{
    public const uint TileSize = 32;

    /// <summary>
    ///     The tile's sides as view-space slopes (x / z and y / z): left, right, bottom, top.
    /// </summary>
    public static Vector4 TileSlopes(uint tileX, uint tileY, uint width, uint height, float tanHalfFovY, float aspect)
    {
        var x0 = tileX * TileSize;
        var y0 = tileY * TileSize;
        var x1 = x0 + TileSize;
        var y1 = y0 + TileSize;
        if (x1 > width) x1 = width;
        if (y1 > height) y1 = height;

        var tanX = tanHalfFovY * aspect;
        // Pixel rows grow downward and NDC -1 is the top row, while view space has +Y up.
        return new Vector4(
            (x0 / (float)width * 2f - 1f) * tanX,
            (x1 / (float)width * 2f - 1f) * tanX,
            -(y1 / (float)height * 2f - 1f) * tanHalfFovY,
            -(y0 / (float)height * 2f - 1f) * tanHalfFovY);
    }

    /// <summary>
    ///     Whether a view-space sphere reaches inside the tile's four side planes. Depth is the z-bins' job.
    /// </summary>
    public static bool SphereTouchesTile(Vector3 center, float radius, Vector4 slopes)
    {
        return !OutsidePlane(center.X - slopes.X * center.Z, slopes.X, radius)
               && !OutsidePlane(slopes.Y * center.Z - center.X, slopes.Y, radius)
               && !OutsidePlane(center.Y - slopes.Z * center.Z, slopes.Z, radius)
               && !OutsidePlane(slopes.W * center.Z - center.Y, slopes.W, radius);
    }

    // A side plane through the eye is n = (1, -slope) in its axis and z. The sphere is outside when its signed distance
    // is below -radius, compared squared so no square root is needed.
    private static bool OutsidePlane(float side, float slope, float radius)
    {
        return side < 0f && side * side > radius * radius * (1f + slope * slope);
    }

    /// <summary>
    ///     The bits of light word <paramref name="word" /> that fall inside the sorted index range [first, last].
    /// </summary>
    public static uint RangeMask(uint word, uint first, uint last)
    {
        var wordStart = word * 32u;
        var low = first > wordStart ? first - wordStart : 0u;
        var high = last - wordStart < 31u ? last - wordStart : 31u;
        return (0xFFFFFFFFu << (int)low) & (0xFFFFFFFFu >> (int)(31u - high));
    }

    /// <summary>
    ///     The z-bin of a view depth. The same formula as <see cref="LightBinner.BinOf" />, with the scale precomputed.
    /// </summary>
    public static uint BinOf(float depth, float near, float binScale)
    {
        if (depth <= near) return 0u;
        var bin = Log(depth / near) * binScale;
        return (uint)Min(bin, LightBinner.BinCount - 1f);
    }
}
