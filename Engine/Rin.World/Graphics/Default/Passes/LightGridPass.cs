using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Builds the view's light grid: bins the lights on the CPU, uploads them, then fills the tile bitmasks on the GPU.
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
        var maxWords = (maxLights + 31) / 32;

        view.LightGridBufferId = config.CreateBuffer<LightGridInfo>(GraphBufferUsage.HostThenCompute);
        view.PointLightsBufferId = config.CreateBuffer<LightInfo>(maxLights, GraphBufferUsage.HostThenCompute);
        view.DirectionalLightsBufferId = config.CreateBuffer<LightInfo>(maxLights, GraphBufferUsage.HostThenGraphics);
        view.LightBinsBufferId = config.CreateBuffer<uint>(LightBinner.BinCount, GraphBufferUsage.HostThenGraphics);
        view.TileMasksBufferId = config.CreateBuffer<uint>((int)(TilesX * TilesY) * maxWords, GraphBufferUsage.Compute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        Binner.Bin(view.Lights, view.View, view.NearClip, view.FarClip);

        var gridBuffer = graph.GetBufferOrException(view.LightGridBufferId);
        var pointLights = graph.GetBufferOrException(view.PointLightsBufferId);
        var directionalLights = graph.GetBufferOrException(view.DirectionalLightsBufferId);
        var bins = graph.GetBufferOrException(view.LightBinsBufferId);
        var tileMasks = graph.GetBufferOrException(view.TileMasksBufferId);

        pointLights.Write(Binner.PointLights);
        directionalLights.Write(Binner.DirectionalLights);
        bins.Write(Binner.Bins);

        var wordCount = (uint)(Binner.PointLights.Length + 31) / 32;
        gridBuffer.WriteSingle(new LightGridInfo
        {
            PointLights = pointLights.ToDeviceSpan<LightInfo>(),
            DirectionalLights = directionalLights.ToDeviceSpan<LightInfo>(),
            Bins = bins.ToDeviceSpan<uint>(),
            TileMasks = tileMasks.ToDeviceSpan<uint>(),
            View = view.View,
            PointLightCount = Binner.PointLights.Length,
            DirectionalLightCount = Binner.DirectionalLights.Length,
            Width = view.Extent.Width,
            Height = view.Extent.Height,
            TilesX = TilesX,
            TilesY = TilesY,
            WordCount = wordCount,
            Near = view.NearClip,
            BinScale = LightBinner.BinCount / float.Log(view.FarClip / view.NearClip),
            TanHalfFovY = float.Tan(float.DegreesToRadians(view.FieldOfView / 2f)),
            Aspect = view.Extent.Width / (float)view.Extent.Height
        });

        if (wordCount == 0 || Shader.Bind(ctx) is not { } bindContext) return;
        bindContext
            .Push(new TileLightsShader.PushConstants { Grid = gridBuffer.ToDeviceSpan<LightGridInfo>() })
            .Invoke(TilesX * TilesY * wordCount);
    }
}
