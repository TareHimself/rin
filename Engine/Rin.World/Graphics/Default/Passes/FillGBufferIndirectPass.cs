using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default.Passes;

public class FillGBufferIndirectPass : IPass
{
    private readonly DefaultWorldCollectedData _collectedData;

    private uint[] _materialBufferIds;
    private uint _worldBufferId;

    public FillGBufferIndirectPass(DefaultWorldCollectedData collectedData)
    {
        _collectedData = collectedData;
    }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        config.ReadTexture(_collectedData.DepthImageId, ImageLayout.DepthAttachment);
        config.WriteTexture(_collectedData.GBufferImage0, ImageLayout.ColorAttachment);
        config.WriteTexture(_collectedData.GBufferImage1, ImageLayout.ColorAttachment);
        config.WriteTexture(_collectedData.GBufferImage2, ImageLayout.ColorAttachment);
        config.WriteTexture(_collectedData.GBufferImage3, ImageLayout.ColorAttachment);
        if (_collectedData.SkinningOutputBufferId > 0)
            config.ReadBuffer(_collectedData.SkinningOutputBufferId, GraphBufferUsage.Graphics);

        _worldBufferId = config.CreateBuffer<WorldInfo>(GraphBufferUsage.HostThenGraphics);

        var indirectGroups = _collectedData.IndirectGroups;
        _collectedData.DeclareDrawResources(config, indirectGroups, material => material.ColorPass);

        _materialBufferIds = new uint[indirectGroups.Count];

        foreach (var (group,i) in indirectGroups.Values.Zip(Enumerable.Range(0,_materialBufferIds.Length)))
        {
            var size = group.First().Material.ColorPass.GetRequiredMemory() * (ulong)group.Count;
            if (size > 0)
                _materialBufferIds[i] = config.CreateBuffer(size,
                    GraphBufferUsage.HostThenGraphics);
        }

        foreach (var id in _collectedData.IndirectCommandBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
        foreach (var id in _collectedData.IndirectCommandCountBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var gBuffer0 = graph.GetImageOrException(_collectedData.GBufferImage0);
        var gBuffer1 = graph.GetImageOrException(_collectedData.GBufferImage1);
        var gBuffer2 = graph.GetImageOrException(_collectedData.GBufferImage2);
        var gBuffer3 = graph.GetImageOrException(_collectedData.GBufferImage3);
        var depthImage = graph.GetImageOrException(_collectedData.DepthImageId);
        var materialDataBuffers = _materialBufferIds.Select(graph.GetBufferOrNull).ToArray();
        var indirectCommandBuffers = _collectedData.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers =
            _collectedData.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var worldBuffer = graph.GetBufferOrException(_worldBufferId);

        var extent = _collectedData.Extent;
        ctx
            .BeginRendering(extent, [gBuffer0, gBuffer1, gBuffer2, gBuffer3], depthImage)
            .EnableBackFaceCulling()
            .DisableDepthWrite();

        var worldFrame = new WorldFrame(_collectedData.View, _collectedData.Projection, worldBuffer, ctx);

        worldBuffer.WriteSingle(new WorldInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection,
            CameraPosition = _collectedData.ViewTransform.Position
        });

        var indirectGroups = _collectedData.IndirectGroups;
        foreach (var (group,i) in indirectGroups.Values.Zip(Enumerable.Range(0,indirectGroups.Count)))
        {
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
            if (firstPass.BindGroup(worldFrame, materialDataBuffer) is { } bindContext)
                bindContext.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Count, 0);
        }

        ctx.EndRendering();
    }
}