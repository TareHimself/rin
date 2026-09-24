using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Initializes the world context and initial buffers
/// </summary>
public class InitWorldResourcesPass : IPass
{
    private readonly DefaultWorldCollectedData _collectedData;

    public InitWorldResourcesPass(DefaultWorldCollectedData collectedData)
    {
        _collectedData = collectedData;
    }

    public void Configure(IGraphConfig config)
    {
        _collectedData.BoundsBufferId = config.CreateBuffer<Bounds3D>(
            _collectedData.ProcessedMeshes.Count,
            GraphBufferUsage.Host);
        _collectedData.GBufferImage0 =
            config.CreateTexture(_collectedData.Extent, ImageFormat.RGBA32, ImageLayout.ShaderAccess);
        _collectedData.GBufferImage1 =
            config.CreateTexture(_collectedData.Extent, ImageFormat.RGBA32, ImageLayout.ShaderAccess);
        _collectedData.GBufferImage2 =
            config.CreateTexture(_collectedData.Extent, ImageFormat.RGBA32, ImageLayout.ShaderAccess);
        _collectedData.GBufferImage3 =
            config.CreateTexture(_collectedData.Extent, ImageFormat.RGBA32, ImageLayout.ShaderAccess);
        _collectedData.DepthImageId =
            config.CreateTexture(_collectedData.Extent, ImageFormat.Depth, ImageLayout.ShaderAccess);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var gBuffer0 = graph.GetImageOrException(_collectedData.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(_collectedData.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(_collectedData.GBufferImage2);
        var gBuffer3 = graph.GetImageOrException(_collectedData.GBufferImage3);
        var depthImage = graph.GetImageOrException(_collectedData.DepthImageId);
        var boundsBuffer = graph.GetBufferOrException(_collectedData.BoundsBufferId);
        boundsBuffer.Write(_collectedData.ProcessedMeshes.Select(c => c.Surface.Bounds).ToArray());
        ctx
            .ClearColorImages(Vector4.Zero, [gBuffer0, gBuffer1, gBuffer2, gBuffer3])
            .ClearDepthImages(0, [depthImage]);
    }

    public uint Id { get; set; }
}