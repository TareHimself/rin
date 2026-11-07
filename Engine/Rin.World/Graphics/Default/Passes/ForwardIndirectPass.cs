using System.Runtime.CompilerServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Forward color for one view that draws the culled batches with indirect draw calls.
/// </summary>
public class ForwardIndirectPass(DefaultWorldViewData view) : IPass
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
        foreach (var id in view.IndirectCommandBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
        foreach (var id in view.IndirectCommandCountBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
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
            ViewProjection = worldFrame.ViewProjection,
            CameraPosition = view.ViewTransform.Position,
            Time = view.Time,
            Grid = new BufferRef<LightGridInfo>(graph.GetBufferOrException(view.LightGridBufferId).GetAddress()),
            Channel = view.Channel
        });

        var i = 0;
        foreach (var group in sceneFrame.IndirectGroups.Values)
        {
            var materialDataBuffer = graph.GetBufferOrNull(sceneFrame.ColorMaterialBufferIds[i]);
            var commandBuffer = graph.GetBufferOrException(view.IndirectCommandBuffers[i]);
            var countBuffer = graph.GetBufferOrException(view.IndirectCommandCountBuffers[i]);
            i++;

            var first = group[0];
            ctx.BindIndexBuffer(first.IndexBuffer);
            if (first.Material.ColorPass.BindGroup(worldFrame, materialDataBuffer) is { } bindContext)
                bindContext.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Count,
                    (uint)Unsafe.SizeOf<DrawIndexedIndirectCommand>());
        }

        ctx.EndRendering();
    }
}
