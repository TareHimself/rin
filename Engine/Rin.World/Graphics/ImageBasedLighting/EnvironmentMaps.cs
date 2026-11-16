using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;

namespace Rin.World.Graphics.ImageBasedLighting;

/// <summary>
///     Uploads environment maps and hands out handles to them, which keeps the managed data out of components.
/// </summary>
public sealed class EnvironmentMaps : IDisposable
{
    private readonly List<DeviceEnvironmentMap?> _maps = [];
    private readonly List<uint> _generations = [];
    private readonly Stack<uint> _freeIndices = new();
    private ResourceHandle _reflectionLut;
    private IGraphicsModule? _graphics;

    /// <summary>
    ///     Creates the map's cubemap and uploads its panorama. The first frame that uses the map blurs the panorama
    ///     into the cubemap on the GPU.
    /// </summary>
    public EnvironmentMapHandle Create(EnvironmentMap map, IGraphicsModule? graphicsModule = null)
    {
        var graphics = _graphics ??= graphicsModule ?? IGraphicsModule.Get();
        var faceSize = (uint)map.FaceSize;
        var cubemap = graphics.CreateCubemap(new Extent2D(faceSize, faceSize), ImageFormat.RGBA16, (uint)map.MipCount,
            ImageCreateFlags.Sampled | ImageCreateFlags.ColorAttachment);

        // The blur reads blurrier panorama mips for rougher surfaces, so the whole chain is built.
        var panorama = graphics.CreateTexture(map.PanoramaExtent, ImageFormat.RGBA16, IGraphicsModule.FullMipChain,
            ImageCreateFlags.Sampled | ImageCreateFlags.TransferDst);
        graphics.QueueImageUpload(panorama, MemoryMarshal.AsBytes(map.Panorama),
            ImageRegion.Whole(map.PanoramaExtent), true);

        if (_reflectionLut.Id == 0) _reflectionLut = ReflectionLut.CreateTexture(graphics);

        return Add(new DeviceEnvironmentMap(cubemap, faceSize, (uint)map.MipCount, map.Irradiance, _reflectionLut,
            panorama));
    }

    public DeviceEnvironmentMap? Get(EnvironmentMapHandle handle)
    {
        return IsLive(handle) ? _maps[(int)handle.Index] : null;
    }

    /// <summary>
    ///     Frees the map's cubemap. Remove every <see cref="EnvironmentLight" /> that uses it first.
    /// </summary>
    public void Free(EnvironmentMapHandle handle)
    {
        if (Get(handle) is not { } map) return;

        _graphics!.FreeResourceHandles(map.Cubemap, map.Panorama);
        _maps[(int)handle.Index] = null;
        _generations[(int)handle.Index]++;
        _freeIndices.Push(handle.Index);
    }

    public void Dispose()
    {
        for (var index = 0u; index < _maps.Count; index++)
            if (_maps[(int)index] is not null)
                Free(new EnvironmentMapHandle(index, _generations[(int)index]));

        if (_reflectionLut.Id == 0) return;
        _graphics!.FreeResourceHandles(_reflectionLut);
        _reflectionLut = default;
    }

    public bool TryDescribe(in EnvironmentLight light, in Matrix4x4 transform, out EnvironmentVolume volume)
    {
        if (Get(light.Map) is not { } map)
        {
            volume = default;
            return false;
        }

        volume = new EnvironmentVolume
        {
            Map = map,
            Intensity = light.Intensity,
            Transform = transform,
            Extent = light.Extent,
            BlendDistance = light.BlendDistance,
            Priority = light.Priority
        };
        return true;
    }

    private EnvironmentMapHandle Add(DeviceEnvironmentMap map)
    {
        if (_freeIndices.TryPop(out var index))
        {
            _maps[(int)index] = map;
        }
        else
        {
            index = (uint)_maps.Count;
            _maps.Add(map);
            _generations.Add(1);
        }

        return new EnvironmentMapHandle(index, _generations[(int)index]);
    }

    private bool IsLive(EnvironmentMapHandle handle)
    {
        return handle.IsValid && handle.Index < _maps.Count && _generations[(int)handle.Index] == handle.Generation;
    }
}
