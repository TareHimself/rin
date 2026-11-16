using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.Default.Shaders;

/// <summary>
///     Turns a panorama into the faces of a cubemap, blurred to match a surface roughness by averaging it over the
///     directions a GGX surface of that roughness reflects when viewed head on.
/// </summary>
[ShadeExport]
public static class EnvironmentFiltering
{
    public const uint SampleCount = 128;

    private const float Pi = 3.14159265359f;
    private const float Tau = 6.28318530718f;

    /// <summary>
    ///     The direction through a point of a cubemap face, where s runs right and t runs down, both in -1 to 1. Faces
    ///     are numbered in <see cref="CubeFace" /> order.
    /// </summary>
    public static Vector3 FaceDirection(int face, float s, float t)
    {
        if (face == 0) return Normalize(new Vector3(1f, -t, -s));
        if (face == 1) return Normalize(new Vector3(-1f, -t, s));
        if (face == 2) return Normalize(new Vector3(s, 1f, t));
        if (face == 3) return Normalize(new Vector3(s, -1f, -t));
        if (face == 4) return Normalize(new Vector3(s, -t, 1f));
        return Normalize(new Vector3(-s, -t, -1f));
    }

    /// <summary>
    ///     Where a direction falls in a panorama with +Z at the centre, +X to its right and +Y along the top row.
    /// </summary>
    public static Vector2 PanoramaCoordinate(Vector3 direction)
    {
        var across = 0.5f + Atan2(direction.X, direction.Z) / Tau;
        var down = Acos(Clamp(direction.Y, -1f, 1f)) / Pi;
        return new Vector2(across, down);
    }

    /// <summary>
    ///     The environment along <paramref name="normal" /> for one roughness. <paramref name="sharpestLevel" /> is the
    ///     panorama mip whose texels match the cubemap texels being written, and <paramref name="panoramaSize" /> its
    ///     width and height at mip 0.
    /// </summary>
    public static Vector3 Blur(DeviceHandle panorama, Vector3 normal, float roughness, float sharpestLevel,
        Vector2 panoramaSize)
    {
        if (roughness <= 0f) return Look(panorama, normal, sharpestLevel, panoramaSize.Y);

        var alpha = roughness * roughness;
        var up = Abs(normal.Y) < 0.999f ? new Vector3(0f, 1f, 0f) : new Vector3(1f, 0f, 0f);
        var tangent = Normalize(Cross(up, normal));
        var bitangent = Cross(normal, tangent);
        var texelSolidAngle = 4f * Pi / (panoramaSize.X * panoramaSize.Y);

        var total = new Vector3(0f);
        var totalWeight = 0f;
        for (var i = 0u; i < SampleCount; i++)
        {
            var spread = RadicalInverse(i);
            var cosHalf = Sqrt((1f - spread) / (1f + (alpha * alpha - 1f) * spread));
            var sinHalf = Sqrt(1f - cosHalf * cosHalf);
            var around = Tau * (i + 0.5f) / SampleCount;

            // The light direction is the head-on view mirrored about the sampled half vector.
            var facing = 2f * cosHalf * cosHalf - 1f;
            if (facing <= 0f) continue;

            var sideways = 2f * cosHalf * sinHalf;
            var light = tangent * (sideways * Cos(around)) + bitangent * (sideways * Sin(around)) + normal * facing;

            // A direction that is sampled rarely stands for a wide patch of sky, so it reads a blurrier mip.
            var spreadTerm = cosHalf * cosHalf * (alpha * alpha - 1f) + 1f;
            var density = alpha * alpha / (Pi * spreadTerm * spreadTerm) / 4f;
            var level = 0.5f * Log2(1f / (SampleCount * density * texelSolidAngle)) + 1f;

            total += Look(panorama, light, Max(level, sharpestLevel), panoramaSize.Y) * facing;
            totalWeight += facing;
        }

        return total / totalWeight;
    }

    private static Vector3 Look(DeviceHandle panorama, Vector3 direction, float level, float panoramaHeight)
    {
        // The sampler wraps, which is right across the image but would blend the top row with the bottom one, so the
        // lookup stays half a texel of this mip away from the poles.
        var poleMargin = Min(0.5f * Exp2(level) / panoramaHeight, 0.5f);
        var coordinate = PanoramaCoordinate(direction);
        coordinate.Y = Clamp(coordinate.Y, poleMargin, 1f - poleMargin);
        return BindlessData.SampleTextureLevel(panorama, coordinate, level, ImageTiling.Repeat, ImageFilter.Linear).XYZ;
    }

    // The bits of the index mirrored around the binary point, an evenly spread sequence in 0 to 1.
    private static float RadicalInverse(uint index)
    {
        var bits = (index << 16) | (index >> 16);
        bits = ((bits & 0x55555555u) << 1) | ((bits & 0xAAAAAAAAu) >> 1);
        bits = ((bits & 0x33333333u) << 2) | ((bits & 0xCCCCCCCCu) >> 2);
        bits = ((bits & 0x0F0F0F0Fu) << 4) | ((bits & 0xF0F0F0F0u) >> 4);
        bits = ((bits & 0x00FF00FFu) << 8) | ((bits & 0xFF00FF00u) >> 8);
        return bits * 2.3283064365386963e-10f;
    }
}
