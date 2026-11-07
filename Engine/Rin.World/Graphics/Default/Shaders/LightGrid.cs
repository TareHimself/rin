using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;
using Rin.World.Graphics.Surfaces;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.Default.Shaders;

/// <summary>
///     One view's light grid: the point lights sorted by view depth, the z-bins from <see cref="LightBinner" />, and one
///     bitmask word per 32 lights per screen tile.
/// </summary>
[ShadeExport]
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
///     What lighting one surface point produced. <see cref="Color" /> is linear, <see cref="Radiance" /> is the incoming
///     light without the surface's response, for the radiance channel.
/// </summary>
[ShadeExport]
public struct LightingResult
{
    public Vector3 Color;
    public Vector3 Radiance;
    public uint LightCount;
}

/// <summary>
///     Grid lookups shared by the tile pass and the shading loop. Everything but <see cref="BinOf" /> is plain
///     arithmetic, so it also runs as ordinary C# in tests.
/// </summary>
[ShadeExport]
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
    ///     Lights a surface point with every directional light and the point lights in both its z-bin and its tile.
    /// </summary>
    public static LightingResult Light(Surface surface, Vector3 location, Vector3 eye, Vector2 pixel, LightGridInfo grid)
    {
        LightingResult result;
        result.Color = LightMath.Rgb2Lin(new Vector3(surface.Emissive));
        result.Radiance = new Vector3(0f);
        result.LightCount = (uint)grid.DirectionalLightCount;
        for (var i = 0; i < grid.DirectionalLightCount; i++)
            Add(ref result, surface, location, eye, grid.DirectionalLights[i]);

        var depth = Vector4.Transform(new Vector4(location, 1f), grid.View).Z;
        var bin = grid.Bins[BinOf(depth, grid.Near, grid.BinScale)];
        var first = bin & 0xFFFFu;
        var last = bin >> 16;
        if (first > last) return result;

        var tile = ((uint)pixel.Y / TileSize * grid.TilesX + (uint)pixel.X / TileSize) * grid.WordCount;
        for (var word = first / 32u; word <= last / 32u; word++)
        {
            var bits = grid.TileMasks[tile + word] & RangeMask(word, first, last);
            result.LightCount += CountBits(bits);
            while (bits != 0u)
            {
                Add(ref result, surface, location, eye, grid.PointLights[word * 32u + FirstBitLow(bits)]);
                bits &= bits - 1u;
            }
        }

        return result;
    }

    /// <summary>
    ///     Blue at zero lights, green at half of <paramref name="hot" />, red at <paramref name="hot" /> or more.
    /// </summary>
    public static Vector3 Heatmap(uint count, float hot)
    {
        var t = Min(count / hot, 1f) * 2f - 1f;
        return new Vector3(Saturate(t), 1f - Abs(t), Saturate(-t));
    }

    private static void Add(ref LightingResult result, Surface surface, Vector3 location, Vector3 eye, LightInfo light)
    {
        result.Color += DisneyModel.Eval(surface, location, eye, light);
        var noL = Max(Dot(surface.Normal, LightMath.DirectionToLight(light, location)), 0f);
        result.Radiance += noL * light.Radiance * light.Color * LightMath.Attenuation(light, location);
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
