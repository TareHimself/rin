using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     G-buffer fill for one view that draws the scene's batches with one CPU draw call per mesh.
///     Used when the device does not support indirect rendering, so nothing is culled.
/// </summary>
public class FillGBufferDirectPass(DefaultWorldViewData view) : IPass
{
    private uint _worldBufferId;

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        var sceneFrame = view.SceneFrame;
        config.ReadTexture(view.DepthImageId, ImageLayout.DepthAttachment);
        config.WriteTexture(view.GBufferImage0, ImageLayout.ColorAttachment);
        config.WriteTexture(view.GBufferImage1, ImageLayout.ColorAttachment);
        config.WriteTexture(view.GBufferImage2, ImageLayout.ColorAttachment);
        config.WriteTexture(view.GBufferImage3, ImageLayout.ColorAttachment);
        if (sceneFrame.SkinningOutputBufferId > 0)
            config.ReadBuffer(sceneFrame.SkinningOutputBufferId, GraphBufferUsage.Graphics);

        _worldBufferId = config.CreateBuffer<WorldInfo>(GraphBufferUsage.HostThenGraphics);

        sceneFrame.DeclareDrawResources(config, sceneFrame.IndirectGroups, material => material.ColorPass);
        foreach (var id in sceneFrame.ColorMaterialBufferIds)
            if (id != 0)
                config.ReadBuffer(id, GraphBufferUsage.Graphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var sceneFrame = view.SceneFrame;
        var gBuffer0 = graph.GetImageOrException(view.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(view.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(view.GBufferImage2);
        var gBuffer3 = graph.GetImageOrException(view.GBufferImage3);
        var depthImage = graph.GetImageOrException(view.DepthImageId);
        var worldBuffer = graph.GetBufferOrException(_worldBufferId);

        ctx
            .BeginRendering(view.Extent, [gBuffer0, gBuffer1, gBuffer2, gBuffer3], depthImage)
            .EnableBackFaceCulling()
            .DisableDepthWrite();

        var worldFrame = new WorldFrame(view.View, view.Projection, worldBuffer, ctx);

        worldBuffer.WriteSingle(new WorldInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection,
            CameraPosition = view.ViewTransform.Position
        });

        var i = 0;
        foreach (var group in sceneFrame.IndirectGroups.Values)
        {
            var materialDataBuffer = graph.GetBufferOrNull(sceneFrame.ColorMaterialBufferIds[i]);
            i++;

            var first = group[0];
            ctx.BindIndexBuffer(first.IndexBuffer);
            if (first.Material.ColorPass.BindGroup(worldFrame, materialDataBuffer) is { } bindContext)
                DirectMeshDraw.DrawGroup(bindContext, group);
        }

        ctx.EndRendering();
    }
}
