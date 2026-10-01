using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Depth pre-pass for one view that draws the scene's shared depth batches with one CPU draw call per mesh.
///     Used when the device does not support indirect rendering, so nothing is culled.
/// </summary>
public class DepthPrepassDirectPass(DefaultWorldViewData view) : IPass
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
            i++;

            var first = group[0];
            ctx.BindIndexBuffer(first.IndexBuffer);
            if (first.Material.DepthPass.BindGroup(worldFrame, materialDataBuffer) is { } bindContext)
                DirectMeshDraw.DrawGroup(bindContext, group);
        }

        ctx.EndRendering();
    }
}
