using Rin.Core.Graphics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     An <see cref="EnvironmentMap" /> on the GPU: its cubemap, which a render pass fills from the panorama the first
///     time a frame uses it, and the irradiance kept on the CPU.
/// </summary>
public sealed class DeviceEnvironmentMap(
    ResourceHandle cubemap,
    uint faceSize,
    uint mipCount,
    in SphericalHarmonics9 irradiance,
    ResourceHandle reflectionLut,
    ResourceHandle panorama)
{
    private readonly SphericalHarmonics9 _irradiance = irradiance;
    private volatile bool _isFiltered;

    public ResourceHandle Cubemap { get; } = cubemap;
    public uint FaceSize { get; } = faceSize;
    public uint MipCount { get; } = mipCount;

    public ResourceHandle ReflectionLut { get; } = reflectionLut;

    /// <summary>
    ///     The mipped panorama the cubemap is filled from. Freed once <see cref="IsFiltered" />.
    /// </summary>
    public ResourceHandle Panorama { get; } = panorama;

    public bool IsFiltered => _isFiltered;

    public ref readonly SphericalHarmonics9 Irradiance => ref _irradiance;

    public void MarkFiltered()
    {
        _isFiltered = true;
    }
}
