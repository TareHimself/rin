using System.Collections.Frozen;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.World.Graphics;

/// <summary>
///     Collects the <see cref="Rin.Engine.World.World" />, performs skinning and does a depth pre-pass
/// </summary>
public class DepthPrepassIndirectPass : IPass
{
    private readonly WorldContext _worldContext;
    
    public DepthPrepassIndirectPass(WorldContext worldContext)
    {
        _worldContext = worldContext;
    }

    [PublicAPI] public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }
    private uint DepthSceneBufferId { get; set; }

    public void PreAdd(IGraphBuilder builder)
    {
        
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }
    
    private uint[] _materialBufferIds = [];
    public void Configure(IGraphConfig config)
    {
        DepthImageId = config.WriteImage(_worldContext.DepthImageId, ImageLayout.DepthAttachment);
        DepthSceneBufferId = config.CreateBuffer<DepthSceneInfo>(GraphBufferUsage.HostThenGraphics);
        
        var indirectGroups = _worldContext.DepthIndirectGroups;
        
        _materialBufferIds = new uint[indirectGroups.Length];
        
        for (var i = 0; i < _materialBufferIds.Length; i++)
        {
            var group = indirectGroups[i];
            var size = group.First().Material.DepthPass.GetRequiredMemory() * (ulong)group.Length;
            if (size > 0)
            {
                _materialBufferIds[i] = config.CreateBuffer(size,
                    GraphBufferUsage.HostThenGraphics);
            }
        }
        
        foreach (var id in _worldContext.DepthIndirectCommandBuffers)
        {
            config.ReadBuffer(id, GraphBufferUsage.Indirect);
        }
        foreach (var id in _worldContext.DepthIndirectCommandCountBuffers)
        {
            config.ReadBuffer(id, GraphBufferUsage.Indirect);
        }
    }


    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        //var cmd = ctx.GetCommandBuffer();
        var worldDataBuffer = graph.GetBufferOrException(DepthSceneBufferId);
        var materialDataBuffers = _materialBufferIds.Select(graph.GetBufferOrNull).ToArray();
        var indirectCommandBuffers = _worldContext.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers = _worldContext.DepthIndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        DepthImage = graph.GetImage(DepthImageId);
        var extent = _worldContext.Extent;
        ctx
            .BeginRendering(extent, [], depthAttachment: DepthImage)
            .EnableBackFaceCulling();

        var worldFrame = new WorldFrame(_worldContext.View, _worldContext.Projection, worldDataBuffer,ctx);

        worldDataBuffer.Write(new DepthSceneInfo
        {
            View = worldFrame.View,
            Projection = worldFrame.Projection,
            ViewProjection = worldFrame.ViewProjection
        });

        var indirectGroups = _worldContext.DepthIndirectGroups;
        for (var i = 0; i < indirectGroups.Length; i++)
        {
            var group = indirectGroups[i];
            var materialDataBuffer = materialDataBuffers[i];
            var commandBuffer = indirectCommandBuffers[i];
            var countBuffer = indirectCommandCountBuffers[i];
            var first = group.First();
            var firstPass = first.Material.DepthPass;
            if (materialDataBuffer != null)
            {
                var dataSize = firstPass.GetRequiredMemory();
                ulong offset = 0;
                foreach (var mesh in group)
                {
                    mesh.Material.DepthPass.Write(materialDataBuffer.GetView(offset,dataSize),mesh);
                    offset += dataSize;
                }
            }
            ctx.BindIndexBuffer(first.IndexBuffer);
            firstPass.BindAndPush(worldFrame,materialDataBuffer);
            ctx.DrawIndexedIndirectCount(commandBuffer, countBuffer, (uint)group.Length, 0);
        }
        ctx.EndRendering();
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = true;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;
    
    public void Dispose()
    {
    }

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