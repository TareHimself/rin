using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.World.Graphics;

public class FillIndirectBuffersDebugPass(WorldContext worldContext) : IComputePass
{
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    private uint[] _bufferIds = [];
    
    
    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }
    
    public void Configure(IGraphConfig config)
    {
        config.DependOn(worldContext.InitPassId);
        worldContext.IndirectCommandBuffers = worldContext.IndirectGroups.Select((group) => config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.HostThenGraphics)).ToArray();
        worldContext.DepthIndirectCommandBuffers = worldContext.DepthIndirectGroups.Select((group) => config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.HostThenGraphics)).ToArray();
        worldContext.IndirectCommandCountBuffers = worldContext.IndirectGroups.Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenGraphics)).ToArray();
        worldContext.DepthIndirectCommandCountBuffers = worldContext.DepthIndirectGroups.Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenGraphics)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var indirectCommandBuffers = worldContext.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers = worldContext.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandBuffers = worldContext.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandCountBuffers = worldContext.DepthIndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        
        for (var i = 0; i < worldContext.IndirectGroups.Length; i++)
        {
            var group = worldContext.IndirectGroups[i];
            var invokeCount = (uint)group.Length;
            var commandBuffer = indirectCommandBuffers[i];
            var countBuffer = indirectCommandCountBuffers[i];
            countBuffer.Write<uint>(invokeCount);
            commandBuffer.Write(group.Select((m, idx) => new VkDrawIndexedIndirectCommand
            {
                instanceCount = 1,
                indexCount = m.IndicesCount,
                firstIndex = m.IndicesStart,
                vertexOffset = (int)m.VertexStart,
                firstInstance = (uint)idx
            }));
        }
        for (var i = 0; i < worldContext.DepthIndirectGroups.Length; i++)
        {
            var group = worldContext.DepthIndirectGroups[i];
            var invokeCount = (uint)group.Length;
            var commandBuffer = depthIndirectCommandBuffers[i];
            var countBuffer = depthIndirectCommandCountBuffers[i];
            countBuffer.Write<uint>(invokeCount);
            commandBuffer.Write(group.Select((m, idx) => new VkDrawIndexedIndirectCommand
            {
                instanceCount = 1,
                indexCount = m.IndicesCount,
                firstIndex = m.IndicesStart,
                vertexOffset = (int)m.VertexStart,
                firstInstance = (uint)idx
            }));
        }
    }
}