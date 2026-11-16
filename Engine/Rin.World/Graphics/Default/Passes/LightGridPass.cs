using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.Core.Shared;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.ImageBasedLighting;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Builds the view's grid: sorts and bins its culled items on the CPU, uploads them, then fills the tile bitmasks
///     on the GPU.
/// </summary>
public partial class LightGridPass(DefaultWorldViewData view) : IComputePass
{
    // Execute only runs on the render thread, one view at a time, so every view can share one binner.
    private static readonly LightBinner Binner = new();

    [ComputeShader<TileLightsShader>]
    private partial IComputeShader Shader { get; }

    public uint Id { get; set; }

    private uint TilesX => (view.Extent.Width + LightGrid.TileSize - 1) / LightGrid.TileSize;
    private uint TilesY => (view.Extent.Height + LightGrid.TileSize - 1) / LightGrid.TileSize;

    public void Configure(IGraphConfig config)
    {
        var maxLights = view.Lights.Length;
        var volumeCount = view.Snapshot.Environments.Length;
        var maxItems = maxLights + volumeCount;
        var maxWords = (maxItems + 31) / 32;

        view.LightGridBufferId = config.CreateBuffer<LightGridInfo>(GraphBufferUsage.HostThenCompute);
        view.ClusterItemsBufferId = config.CreateBuffer<ClusterItem>(maxItems, GraphBufferUsage.HostThenCompute);
        view.PointLightsBufferId = config.CreateBuffer<LightInfo>(maxLights, GraphBufferUsage.HostThenGraphics);
        view.DirectionalLightsBufferId = config.CreateBuffer<LightInfo>(maxLights, GraphBufferUsage.HostThenGraphics);
        view.EnvironmentVolumesBufferId =
            config.CreateBuffer<EnvironmentVolumeInfo>(int.Max(volumeCount, 1), GraphBufferUsage.HostThenGraphics);
        view.LightBinsBufferId = config.CreateBuffer<uint>(ClusterBinner.TypeCount * ClusterBinner.BinCount,
            GraphBufferUsage.HostThenGraphics);
        view.TileMasksBufferId = config.CreateBuffer<uint>((int)(TilesX * TilesY) * maxWords, GraphBufferUsage.Compute);
    }

    // Every volume is written, unbounded ones too, so a cluster item's index is its position in the snapshot.
    private static void WriteVolumes(in DeviceBufferView buffer, ReadOnlySpan<EnvironmentVolume> environments)
    {
        if (environments.IsEmpty) return;

        using var infos = new PooledMemory<EnvironmentVolumeInfo>(environments.Length);
        for (var i = 0; i < environments.Length; i++) infos[i] = environments[i].ToInfo();
        buffer.Write(infos);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var environments = view.Snapshot.Environments;
        Binner.Bin(view.Lights, environments, view.View, view.NearClip, view.FarClip);

        var clusters = Binner.Clusters;
        var gridBuffer = graph.GetBufferOrException(view.LightGridBufferId);
        var items = graph.GetBufferOrException(view.ClusterItemsBufferId);
        var pointLights = graph.GetBufferOrException(view.PointLightsBufferId);
        var directionalLights = graph.GetBufferOrException(view.DirectionalLightsBufferId);
        var volumes = graph.GetBufferOrException(view.EnvironmentVolumesBufferId);
        var bins = graph.GetBufferOrException(view.LightBinsBufferId);
        var tileMasks = graph.GetBufferOrException(view.TileMasksBufferId);

        // Cluster items index the view's lights as they are, so the whole span is the point light array.
        items.Write(clusters.Items);
        pointLights.Write(view.Lights);
        directionalLights.Write(Binner.DirectionalLights);
        bins.Write(clusters.Bins);
        WriteVolumes(volumes, environments);

        var wordCount = (uint)(clusters.Items.Length + 31) / 32;
        gridBuffer.WriteSingle(new LightGridInfo
        {
            Items = items.ToDeviceSpan<ClusterItem>(),
            Bins = bins.ToDeviceSpan<uint>(),
            TileMasks = tileMasks.ToDeviceSpan<uint>(),
            PointLights = pointLights.ToDeviceSpan<LightInfo>(),
            DirectionalLights = directionalLights.ToDeviceSpan<LightInfo>(),
            Volumes = volumes.ToDeviceSpan<EnvironmentVolumeInfo>(),
            View = view.View,
            ItemCount = clusters.Items.Length,
            DirectionalLightCount = Binner.DirectionalLights.Length,
            Width = view.Extent.Width,
            Height = view.Extent.Height,
            TilesX = TilesX,
            TilesY = TilesY,
            WordCount = wordCount,
            Near = view.NearClip,
            BinScale = ClusterBinner.BinScale(view.NearClip, view.FarClip),
            TanHalfFovY = float.Tan(float.DegreesToRadians(view.FieldOfView / 2f)),
            Aspect = view.Extent.Width / (float)view.Extent.Height
        });

        if (wordCount == 0 || Shader.Bind(ctx) is not { } bindContext) return;
        bindContext
            .Push(new TileLightsShader.PushConstants { Grid = gridBuffer.ToDeviceSpan<LightGridInfo>() })
            .Invoke(TilesX * TilesY * wordCount);
    }
}
