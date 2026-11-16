using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;
using Rin.World.Graphics.Surfaces;
using static Rin.Shade.Shader;

namespace Rin.World.Graphics.Default.Shaders;

[ShadeExport]
public static class EnvironmentLighting
{
    private const float ReciprocalPi = 0.3183098861837907f;

    public static Vector3 Irradiance(IrradianceCoefficients coefficients, Vector3 normal)
    {
        var x = normal.X;
        var y = normal.Y;
        var z = normal.Z;
        var irradiance = coefficients.L00 * 0.282095f
                         + coefficients.L1M1 * (0.488603f * y)
                         + coefficients.L10 * (0.488603f * z)
                         + coefficients.L11 * (0.488603f * x)
                         + coefficients.L2M2 * (1.092548f * x * y)
                         + coefficients.L2M1 * (1.092548f * y * z)
                         + coefficients.L20 * (0.315392f * (3f * z * z - 1f))
                         + coefficients.L21 * (1.092548f * x * z)
                         + coefficients.L22 * (0.546274f * (x * x - y * y));
        return Max(irradiance, new Vector3(0f));
    }

    /// <summary>
    ///     Everything the environment adds to a surface at <paramref name="location" /> seen from
    ///     <paramref name="view" /> (the direction to the eye): the environment mirrored in it, blurred by its
    ///     roughness, plus the diffuse light that is not mirrored.
    /// </summary>
    public static Vector3 Ambient(Surface surface, Vector3 location, Vector3 view, Vector2 pixel,
        EnvironmentInfo environment, LightGridInfo grid)
    {
        if (environment.ReflectionLut.Id == 0u) return new Vector3(0f);

        var mirrorDirection = Reflect(-view, surface.Normal);
        var arriving = Irradiance(environment.Irradiance, surface.Normal);
        var mirrored = Sample(environment.Fallback, mirrorDirection, surface.Roughness);

        // The grid hands volumes over nearest first, so the highest priority is tracked as they come.
        var topPriority = 0;
        var weightSum = 0f;
        var arrivingSum = new Vector3(0f);
        var mirroredSum = new Vector3(0f);

        var bin = LightGrid.BinAt(grid, ClusterItemType.EnvironmentVolume, location);
        var first = bin & 0xFFFFu;
        var last = bin >> 16;
        if (first <= last)
        {
            var tile = LightGrid.TileAt(grid, pixel);
            for (var word = first / 32u; word <= last / 32u; word++)
            {
                var bits = grid.TileMasks[tile + word] & LightGrid.RangeMask(word, first, last);
                while (bits != 0u)
                {
                    var volume = grid.Volumes[grid.Items[word * 32u + FirstBitLow(bits)].Index];
                    bits &= bits - 1u;

                    var local = Vector4.Transform(new Vector4(location, 1f), volume.WorldToLocal).XYZ;
                    var weight = BoxWeight(local, volume.Extent, volume.BlendDistance);
                    var isFirst = weightSum <= 0f;
                    if (weight <= 0f || (!isFirst && volume.Priority < topPriority)) continue;

                    if (isFirst || volume.Priority > topPriority)
                    {
                        topPriority = volume.Priority;
                        weightSum = 0f;
                        arrivingSum = new Vector3(0f);
                        mirroredSum = new Vector3(0f);
                    }

                    var direction = ProjectOntoBox(volume, local, mirrorDirection);
                    weightSum += weight;
                    arrivingSum += Irradiance(volume.Irradiance, surface.Normal) * weight;
                    mirroredSum += Sample(volume.Reflection, direction, surface.Roughness) * weight;
                }
            }
        }

        if (weightSum > 0f)
        {
            var coverage = Min(weightSum, 1f);
            arriving = Lerp(arriving, arrivingSum / weightSum, coverage);
            mirrored = Lerp(mirrored, mirroredSum / weightSum, coverage);
        }

        var facing = Saturate(Dot(surface.Normal, view));
        var response = BindlessData.SampleTexture(environment.ReflectionLut, new Vector2(facing, surface.Roughness),
            ImageTiling.ClampEdge, ImageFilter.Linear);
        var reflectivity = LightMath.BaseReflectivity(surface) * response.X + new Vector3(response.Y);

        var diffuse = surface.BaseColor * (1f - surface.Metallic) * arriving * ReciprocalPi;
        return diffuse * (new Vector3(1f) - reflectivity) + mirrored * reflectivity;
    }

    public static Vector3 Backdrop(EnvironmentInfo environment, Vector3 direction)
    {
        var fallback = Sample(environment.Fallback, direction, 0f);
        if (environment.VolumeWeight <= 0f) return fallback;
        return Lerp(fallback, Sample(environment.Volume, direction, 0f), environment.VolumeWeight);
    }

    /// <summary>
    ///     How much a box applies at a point in its local space: 0 outside, rising to 1 once
    ///     <paramref name="blendDistance" /> inside the border.
    /// </summary>
    public static float BoxWeight(Vector3 local, Vector3 extent, float blendDistance)
    {
        var inside = extent - Abs(local);
        var depth = Min(inside.X, Min(inside.Y, inside.Z));
        if (depth < 0f) return 0f;
        return blendDistance > 0f ? Min(depth / blendDistance, 1f) : 1f;
    }

    /// <summary>
    ///     Corrects a lookup direction for a point that is not at the box's centre, by treating the cubemap as painted
    ///     on the box's walls: follow the ray to the wall it hits, then look up the direction from the centre to that hit.
    /// </summary>
    public static Vector3 ProjectOntoBox(EnvironmentVolumeInfo volume, Vector3 local, Vector3 direction)
    {
        var localDirection = Vector4.Transform(new Vector4(direction, 0f), volume.WorldToLocal).XYZ;
        var farWalls = Max((volume.Extent - local) / localDirection, (-volume.Extent - local) / localDirection);
        var distance = Min(farWalls.X, Min(farWalls.Y, farWalls.Z));
        var hit = local + localDirection * distance;
        return Normalize(Vector4.Transform(new Vector4(hit, 0f), volume.LocalToWorld).XYZ);
    }

    private static Vector3 Sample(EnvironmentReflection reflection, Vector3 direction, float roughness)
    {
        if (reflection.Cubemap.Id == 0u) return new Vector3(0f);
        var texel = BindlessData.SampleCubemap(reflection.Cubemap, direction, roughness * reflection.MaxMip);
        return texel.XYZ * reflection.Intensity;
    }
}
