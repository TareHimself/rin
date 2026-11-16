using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Tests.Graphics.ImageBasedLighting;

/// <summary>
///     Builds uploaded maps and volumes without a GPU, each map lit by one constant colour.
/// </summary>
internal static class TestEnvironments
{
    private const int FaceSize = 16;

    public static readonly ResourceHandle ReflectionLutHandle = new(ResourceType.Texture, 900, true, 1);

    public static EnvironmentMap Map(Vector3 colour)
    {
        return EnvironmentMap.FromEquirect(TestEquirect.Constant(colour), TestEquirect.Extent, FaceSize);
    }

    public static DeviceEnvironmentMap DeviceMap(uint cubemapId, Vector3 colour)
    {
        var irradiance = new SphericalHarmonics9 { [0] = colour };
        return new DeviceEnvironmentMap(new ResourceHandle(ResourceType.Cubemap, cubemapId, true, 1), FaceSize, 2,
            irradiance, ReflectionLutHandle, new ResourceHandle(ResourceType.Texture, 1000 + cubemapId, true, 1));
    }

    public static EnvironmentVolume Unbounded(uint cubemapId, Vector3 colour, int priority = 0)
    {
        return new EnvironmentVolume
        {
            Map = DeviceMap(cubemapId, colour),
            Intensity = 1f,
            Transform = Matrix4x4.Identity,
            Priority = priority
        };
    }

    public static EnvironmentVolume Box(uint cubemapId, Vector3 colour, Vector3 centre, float halfSize,
        float blendDistance = 0f, int priority = 0)
    {
        return new EnvironmentVolume
        {
            Map = DeviceMap(cubemapId, colour),
            Intensity = 1f,
            Transform = Matrix4x4.CreateTranslation(centre),
            Extent = new Vector3(halfSize),
            BlendDistance = blendDistance,
            Priority = priority
        };
    }
}
