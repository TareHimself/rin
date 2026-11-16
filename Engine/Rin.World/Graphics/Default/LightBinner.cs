using System.Numerics;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Graphics.Default;

/// <summary>
///     Splits one view's lights and environment volumes into what the grid culls and what every fragment applies:
///     point lights and box volumes go into <see cref="Clusters" /> by their sphere, while directional lights and
///     unbounded environments, which reach everywhere, are left out.
/// </summary>
/// <remarks>
///     A cluster item indexes the span its kind was given to <see cref="Bin" /> in, so those spans are uploaded
///     unchanged as the data arrays. Arrays grow with the light count and are reused.
/// </remarks>
public sealed class LightBinner
{
    private LightInfo[] _directionalLights = [];
    private int _directionalCount;

    public ClusterBinner Clusters { get; } = new();

    public ReadOnlySpan<LightInfo> DirectionalLights => _directionalLights.AsSpan(0, _directionalCount);

    public void Bin(ReadOnlySpan<LightInfo> lights, ReadOnlySpan<EnvironmentVolume> volumes, in Matrix4x4 view,
        float near, float far)
    {
        if (_directionalLights.Length < lights.Length) _directionalLights = new LightInfo[lights.Length];
        _directionalCount = 0;

        Clusters.Begin(view, near, far);
        for (var i = 0; i < lights.Length; i++)
        {
            ref readonly var light = ref lights[i];
            if (light.LightType == LightType.Directional)
                _directionalLights[_directionalCount++] = light;
            else
                Clusters.Add(ClusterItemType.PointLight, (uint)i, light.Location, light.Radius);
        }

        for (var i = 0; i < volumes.Length; i++)
        {
            ref readonly var volume = ref volumes[i];
            if (!volume.IsUnbounded)
                Clusters.Add(ClusterItemType.EnvironmentVolume, (uint)i, volume.Center, volume.BoundingRadius);
        }

        Clusters.Build();
    }
}
