using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.World.Graphics.Default.Passes;

/// <summary>
///     Initializes the world context and initial buffers
/// </summary>
public class InitWorldPass : IPass
{
    private readonly DefaultWorldRenderContext _renderContext;

    public InitWorldPass(DefaultWorldRenderContext renderContext)
    {
        _renderContext = renderContext;
    }

    public void Configure(IGraphConfig config)
    {
        _renderContext.Initialize(Id);
        _renderContext.BoundsBufferId = config.CreateBuffer<Bounds3D>(
            _renderContext.ProcessedSkinnedMeshes.Length + _renderContext.ProcessedStaticMeshes.Length,
            GraphBufferUsage.Host);
        _renderContext.GBufferImage0 =
            config.CreateImage(_renderContext.Extent, ImageFormat.RGBA32, ImageLayout.General);
        _renderContext.GBufferImage1 =
            config.CreateImage(_renderContext.Extent, ImageFormat.RGBA32, ImageLayout.General);
        _renderContext.GBufferImage2 =
            config.CreateImage(_renderContext.Extent, ImageFormat.RGBA32, ImageLayout.General);
        _renderContext.DepthImageId =
            config.CreateImage(_renderContext.Extent, ImageFormat.Depth, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var gBuffer0 = graph.GetImageOrException(_renderContext.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(_renderContext.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(_renderContext.GBufferImage2);
        var depthImage = graph.GetImageOrException(_renderContext.DepthImageId);
        var boundsBuffer = graph.GetBufferOrException(_renderContext.BoundsBufferId);
        boundsBuffer.WriteArray(_renderContext.ProcessedMeshes.Select(c => c.Bounds));
        ctx
            .ClearColorImages(Vector4.Zero, ImageLayout.General, gBuffer0, gBuffer1, gBuffer2)
            .ClearDepthImages(0, ImageLayout.General, depthImage);
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = false;
}