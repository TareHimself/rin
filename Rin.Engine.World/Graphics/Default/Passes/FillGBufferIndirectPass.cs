using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.World.Graphics.Default.Passes;

public class FillGBufferIndirectPass : IPass
{
    private readonly DefaultWorldRenderContext _renderContext;

    private uint[] _materialBufferIds;
    private uint _worldBufferId;

    public FillGBufferIndirectPass(DefaultWorldRenderContext renderContext)
    {
        _renderContext = renderContext;
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(_renderContext.DepthImageId, ImageLayout.DepthAttachment);
        config.WriteImage(_renderContext.GBufferImage0, ImageLayout.ColorAttachment);
        config.WriteImage(_renderContext.GBufferImage1, ImageLayout.ColorAttachment);
        config.WriteImage(_renderContext.GBufferImage2, ImageLayout.ColorAttachment);
        if (_renderContext.SkinningOutputBufferId > 0)
            config.ReadBuffer(_renderContext.SkinningOutputBufferId, GraphBufferUsage.Graphics);

        _worldBufferId = config.CreateBuffer<WorldInfo>(GraphBufferUsage.HostThenGraphics);

        var indirectGroups = _renderContext.IndirectGroups;

        _materialBufferIds = new uint[indirectGroups.Length];

        for (var i = 0; i < _materialBufferIds.Length; i++)
        {
            var group = indirectGroups[i];
            var size = group.First().Material.ColorPass.GetRequiredMemory() * (ulong)group.Length;
            if (size > 0)
                _materialBufferIds[i] = config.CreateBuffer(size,
                    GraphBufferUsage.HostThenGraphics);
        }

        foreach (var id in _renderContext.IndirectCommandBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
        foreach (var id in _renderContext.IndirectCommandCountBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var gBuffer0 = graph.GetImageOrException(_renderContext.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(_renderContext.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(_renderContext.GBufferImage2);
        var depthImage = graph.GetImageOrException(_renderContext.DepthImageId);
        var materialDataBuffers = _materialBufferIds.Select(graph.GetBufferOrNull).ToArray();
        var indirectCommandBuffers = _renderContext.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers =
            _renderContext.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var worldBuffer = graph.GetBufferOrException(_worldBufferId);

        var extent = _renderContext.Extent;
        ctx
            .BeginRendering(extent, [gBuffer0, gBuffer1, gBuffer2], depthImage)
            .EnableBackFaceCulling()
            .DisableDepthWrite();

        var worldFrame = new WorldFrame(_renderContext.View, _renderContext.Projection, worldBuffer, ctx);

        worldBuffer.WriteStruct(new WorldInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection,
            CameraPosition = _renderContext.ViewTransform.Position
        });

        var indirectGroups = _renderContext.IndirectGroups;
        for (var i = 0; i < indirectGroups.Length; i++)
        {
            var group = indirectGroups[i];
            var materialDataBuffer = materialDataBuffers[i];
            var commandBuffer = indirectCommandBuffers[i];
            var countBuffer = indirectCommandCountBuffers[i];
            var first = group.First();
            var firstPass = first.Material.ColorPass;
            if (materialDataBuffer.IsValid)
            {
                var dataSize = firstPass.GetRequiredMemory();
                ulong offset = 0;
                foreach (var mesh in group)
                {
                    mesh.Material.ColorPass.Write(materialDataBuffer.GetView(offset, dataSize), mesh);
                    offset += dataSize;
                }
            }

            ctx.BindIndexBuffer(first.IndexBuffer);
            if (firstPass.BindAndPush(worldFrame, materialDataBuffer))
                ctx.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Length, 0);
        }

        ctx.EndRendering();
    }
}