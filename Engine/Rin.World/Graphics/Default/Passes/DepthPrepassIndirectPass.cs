using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Collects the <see cref="Rin.Core.World.World" />, performs skinning and does a depth pre-pass
/// </summary>
public class DepthPrepassIndirectPass : IPass
{
    private readonly DefaultWorldCollectedData _collectedData;


    private uint[] _materialBufferIds = [];

    public DepthPrepassIndirectPass(DefaultWorldCollectedData collectedData)
    {
        _collectedData = collectedData;
    }

    [PublicAPI] public uint DepthImageId { get; private set; }

    [PublicAPI] public ResourceHandle? DepthImage { get; set; }
    private uint DepthSceneBufferId { get; set; }

    public void Configure(IGraphConfig config)
    {
        DepthImageId = config.WriteTexture(_collectedData.DepthImageId, ImageLayout.DepthAttachment);
        DepthSceneBufferId = config.CreateBuffer<DepthSceneInfo>(GraphBufferUsage.HostThenGraphics);

        // Skinned meshes' vertex buffers are views into SkinningOutputBufferId (reassigned in
        // SkinningPass.Execute) - without declaring the read here, the graph has no dependency edge
        // on SkinningPass's write, so this pass's GPU commands can race ahead of the compute shader
        // that produces the data, corrupting depth for skinned geometry (and anything depth-tested
        // against it) on frames where a skinned mesh exists. FillGBufferIndirectPass already does this.
        if (_collectedData.SkinningOutputBufferId > 0)
            config.ReadBuffer(_collectedData.SkinningOutputBufferId, GraphBufferUsage.Graphics);

        var indirectGroups = _collectedData.DepthIndirectGroups;
        _collectedData.DeclareDrawResources(config, indirectGroups, material => material.DepthPass);

        _materialBufferIds = new uint[indirectGroups.Count];

        {
            foreach (var (group,i) in indirectGroups.Values.Zip(Enumerable.Range(0,_materialBufferIds.Length)))
            {
                var size = group.First().Material.DepthPass.GetRequiredMemory() * (ulong)group.Count;
                if (size > 0)
                    _materialBufferIds[i] = config.CreateBuffer(size,
                        GraphBufferUsage.HostThenGraphics);
            }
        }

        foreach (var id in _collectedData.DepthIndirectCommandBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
        foreach (var id in _collectedData.DepthIndirectCommandCountBuffers)
            config.ReadBuffer(id, GraphBufferUsage.Indirect);
    }


    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        //var cmd = ctx.GetCommandBuffer();
        var worldDataBuffer = graph.GetBufferOrException(DepthSceneBufferId);
        var materialDataBuffers = _materialBufferIds.Select(graph.GetBufferOrNull).ToArray();
        var indirectCommandBuffers =
            _collectedData.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers = _collectedData.DepthIndirectCommandCountBuffers
            .Select(graph.GetBufferOrException).ToArray();
        DepthImage = graph.GetImageOrException(DepthImageId);
        var extent = _collectedData.Extent;
        ctx
            .BeginRendering(extent, [], DepthImage.Value)
            .EnableBackFaceCulling();

        var worldFrame = new WorldFrame(_collectedData.View, _collectedData.Projection, worldDataBuffer, ctx);

        worldDataBuffer.WriteSingle(new DepthSceneInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection
        });

        var indirectGroups = _collectedData.DepthIndirectGroups;
        foreach (var (group,i) in indirectGroups.Values.Zip(Enumerable.Range(0,indirectGroups.Count)))
        {
            var materialDataBuffer = materialDataBuffers[i];
            var commandBuffer = indirectCommandBuffers[i];
            var countBuffer = indirectCommandCountBuffers[i];
            var first = group.First();
            var firstPass = first.Material.DepthPass;
            if (materialDataBuffer.IsValid)
            {
                var dataSize = firstPass.GetRequiredMemory();
                ulong offset = 0;
                foreach (var mesh in group)
                {
                    mesh.Material.DepthPass.Write(materialDataBuffer.GetView(offset, dataSize), mesh);
                    offset += dataSize;
                }
            }

            ctx.BindIndexBuffer(first.IndexBuffer);
            if (firstPass.BindGroup(worldFrame, materialDataBuffer) is { } bindContext)
                bindContext.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Count, 0);
        }

        ctx.EndRendering();
    }

    public uint Id { get; set; }
}