using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Depth pre-pass for one view, drawing the scene's shared depth batches with this view's indirect commands
/// </summary>
public class DepthPrepassIndirectPass(DefaultWorldViewData view) : IPass
{
    [PublicAPI] public uint DepthImageId { get; private set; }

    [PublicAPI] public ResourceHandle? DepthImage { get; set; }
    private uint DepthSceneBufferId { get; set; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        var sceneFrame = view.SceneFrame;
        DepthImageId = config.WriteTexture(view.DepthImageId, ImageLayout.DepthAttachment);
        DepthSceneBufferId = config.CreateBuffer<DepthSceneInfo>(GraphBufferUsage.HostThenGraphics);

        // Skinned vertex buffers are views into SkinningOutputBufferId; this read orders the draws after skinning.
        if (sceneFrame.SkinningOutputBufferId > 0)
            config.ReadBuffer(sceneFrame.SkinningOutputBufferId, GraphBufferUsage.Graphics);

        sceneFrame.DeclareDrawResources(config, sceneFrame.DepthIndirectGroups, material => material.DepthPass);
        foreach (var id in sceneFrame.DepthMaterialBufferIds)
            if (id != 0)
                config.ReadBuffer(id, GraphBufferUsage.Graphics);
        foreach (var id in view.DepthIndirectCommandBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
        foreach (var id in view.DepthIndirectCommandCountBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var sceneFrame = view.SceneFrame;
        var worldDataBuffer = graph.GetBufferOrException(DepthSceneBufferId);
        DepthImage = graph.GetImageOrException(DepthImageId);
        ctx
            .BeginRendering(view.Extent, [], DepthImage.Value)
            .EnableBackFaceCulling();

        var worldFrame = new WorldFrame(view.View, view.Projection, worldDataBuffer, ctx);

        worldDataBuffer.WriteSingle(new DepthSceneInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection
        });

        var i = 0;
        foreach (var group in sceneFrame.DepthIndirectGroups.Values)
        {
            var materialDataBuffer = graph.GetBufferOrNull(sceneFrame.DepthMaterialBufferIds[i]);
            var commandBuffer = graph.GetBufferOrException(view.DepthIndirectCommandBuffers[i]);
            var countBuffer = graph.GetBufferOrException(view.DepthIndirectCommandCountBuffers[i]);
            i++;

            var first = group[0];
            ctx.BindIndexBuffer(first.IndexBuffer);
            if (first.Material.DepthPass.BindGroup(worldFrame, materialDataBuffer) is { } bindContext)
                bindContext.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Count, 0);
        }

        ctx.EndRendering();
    }
}
