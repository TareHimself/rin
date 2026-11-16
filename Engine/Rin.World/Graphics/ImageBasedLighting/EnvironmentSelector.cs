using System.Numerics;
using Rin.Core.Graphics;

namespace Rin.World.Graphics.ImageBasedLighting;

public struct ViewEnvironment
{
    public EnvironmentInfo Info;
    public ResourceHandle VolumeCubemap;
    public ResourceHandle FallbackCubemap;
    public ResourceHandle ReflectionLut;

    public readonly bool HasCubemap => VolumeCubemap.Id != 0 || FallbackCubemap.Id != 0;
}

/// <summary>
///     Picks what a view takes from its position: the highest priority unbounded light, which lights every pixel, and
///     the highest priority box the camera is inside, which the backdrop blends towards. Surfaces find their own boxes
///     per pixel through the grid.
/// </summary>
public static class EnvironmentSelector
{
    private const int None = -1;

    public static ViewEnvironment Select(ReadOnlySpan<EnvironmentVolume> volumes, Vector3 viewPosition)
    {
        var environment = new ViewEnvironment();
        var fallback = FindFallback(volumes);
        if (fallback != None)
        {
            ref readonly var chosen = ref volumes[fallback];
            environment.FallbackCubemap = chosen.Map.Cubemap;
            environment.ReflectionLut = chosen.Map.ReflectionLut;
            environment.Info.Fallback = chosen.ToReflection();
            environment.Info.Irradiance = chosen.Map.Irradiance.Scaled(chosen.Intensity).ToCoefficients();
        }

        var volume = FindVolume(volumes, viewPosition, out var weight);
        if (volume != None)
        {
            ref readonly var chosen = ref volumes[volume];
            environment.VolumeCubemap = chosen.Map.Cubemap;
            environment.ReflectionLut = chosen.Map.ReflectionLut;
            environment.Info.Volume = chosen.ToReflection();
            environment.Info.VolumeWeight = weight;
        }

        if (environment.ReflectionLut.Id == 0 && volumes.Length > 0)
            environment.ReflectionLut = volumes[0].Map.ReflectionLut;
        environment.Info.ReflectionLut = environment.ReflectionLut;
        return environment;
    }

    private static int FindFallback(ReadOnlySpan<EnvironmentVolume> volumes)
    {
        var best = None;
        for (var i = 0; i < volumes.Length; i++)
            if (volumes[i].IsUnbounded && (best == None || volumes[i].Priority > volumes[best].Priority))
                best = i;
        return best;
    }

    private static int FindVolume(ReadOnlySpan<EnvironmentVolume> volumes, Vector3 viewPosition, out float bestWeight)
    {
        var best = None;
        bestWeight = 0f;
        for (var i = 0; i < volumes.Length; i++)
        {
            if (volumes[i].IsUnbounded) continue;

            var weight = volumes[i].WeightAt(viewPosition);
            if (weight <= 0f) continue;
            if (best != None && volumes[i].Priority < volumes[best].Priority) continue;
            if (best != None && volumes[i].Priority == volumes[best].Priority && weight <= bestWeight) continue;

            best = i;
            bestWeight = weight;
        }

        return best;
    }

}
