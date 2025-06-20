using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.World.Graphics.Default.Passes;

/// <summary>
///     Collects the <see cref="Rin.Engine.World.World" />, performs skinning and does a depth pre-pass
/// </summary>
public class DepthPrepassIndirectPass : IPass
{
    private readonly DefaultWorldRenderContext _renderContext;


    private uint[] _materialBufferIds = [];

    public DepthPrepassIndirectPass(DefaultWorldRenderContext renderContext)
    {
        _renderContext = renderContext;
    }

    [PublicAPI] public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }
    private uint DepthSceneBufferId { get; set; }

    public void Configure(IGraphConfig config)
    {
        DepthImageId = config.WriteImage(_renderContext.DepthImageId, ImageLayout.DepthAttachment);
        DepthSceneBufferId = config.CreateBuffer<DepthSceneInfo>(GraphBufferUsage.HostThenGraphics);

        var indirectGroups = _renderContext.DepthIndirectGroups;

        _materialBufferIds = new uint[indirectGroups.Length];

        for (var i = 0; i < _materialBufferIds.Length; i++)
        {
            var group = indirectGroups[i];
            var size = group.First().Material.DepthPass.GetRequiredMemory() * (ulong)group.Length;
            if (size > 0)
                _materialBufferIds[i] = config.CreateBuffer(size,
                    GraphBufferUsage.HostThenGraphics);
        }

        foreach (var id in _renderContext.DepthIndirectCommandBuffers) config.ReadBuffer(id, GraphBufferUsage.Indirect);
        foreach (var id in _renderContext.DepthIndirectCommandCountBuffers)
            config.ReadBuffer(id, GraphBufferUsage.Indirect);
    }


    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        //var cmd = ctx.GetCommandBuffer();
        var worldDataBuffer = graph.GetBufferOrException(DepthSceneBufferId);
        var materialDataBuffers = _materialBufferIds.Select(graph.GetBufferOrNull).ToArray();
        var indirectCommandBuffers =
            _renderContext.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers = _renderContext.DepthIndirectCommandCountBuffers
            .Select(graph.GetBufferOrException).ToArray();
        DepthImage = graph.GetImage(DepthImageId);
        var extent = _renderContext.Extent;
        ctx
            .BeginRendering(extent, [], DepthImage)
            .EnableBackFaceCulling();

        var worldFrame = new WorldFrame(_renderContext.View, _renderContext.Projection, worldDataBuffer, ctx);

        worldDataBuffer.WriteStruct(new DepthSceneInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection
        });

        var indirectGroups = _renderContext.DepthIndirectGroups;
        for (var i = 0; i < indirectGroups.Length; i++)
        {
            var group = indirectGroups[i];
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
            firstPass.BindAndPush(worldFrame, materialDataBuffer);
            ctx.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Length, 0);
        }

        ctx.EndRendering();
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = true;

    private struct SkinningExecutionInfo
    {
        public required int PoseId;
        public required int VertexId;
        public required int MeshId;
    }

    public record struct SkinningPushConstants
    {
        public required ulong ExecutionInfoBuffer;
        public required ulong MeshesBuffer;
        public required ulong OutputBuffer;
        public required ulong PosesBuffer;
        public required int TotalInvocations;
    }
}