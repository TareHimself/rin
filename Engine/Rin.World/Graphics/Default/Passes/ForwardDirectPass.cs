using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Forward color for one view that draws the scene's batches with one CPU draw call per mesh.
///     Used when the device does not support indirect rendering, so nothing is culled.
/// </summary>
public class ForwardDirectPass(DefaultWorldViewData view) : IPass
{
    private uint _worldBufferId;

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        var sceneFrame = view.SceneFrame;
        config.ReadTexture(view.DepthImageId, ImageLayout.DepthAttachment);
        config.WriteTexture(view.OutputImageId, ImageLayout.ColorAttachment);
        view.ReadLightGrid(config);
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
        var outputImage = graph.GetImageOrException(view.OutputImageId);
        var depthImage = graph.GetImageOrException(view.DepthImageId);
        var worldBuffer = graph.GetBufferOrException(_worldBufferId);

        ctx
            .BeginRendering(view.Extent, [outputImage], depthImage)
            .EnableBackFaceCulling()
            .DisableDepthWrite();

        var worldFrame = new WorldFrame(view.View, view.Projection, worldBuffer, ctx);

        worldBuffer.WriteSingle(new WorldInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            CameraPosition = view.ViewTransform.Position,
            Time = view.Time,
            Grid = graph.GetBufferOrException(view.LightGridBufferId).ToDeviceSpan<LightGridInfo>(),
            Channel = view.Channel
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
