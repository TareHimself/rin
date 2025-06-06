using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics.Default.Passes;

public class FillIndirectBuffersDebugPass(DefaultWorldRenderContext renderContext) : IComputePass
{
    private uint[] _bufferIds = [];
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        config.DependOn(renderContext.InitPassId);
        renderContext.IndirectCommandBuffers = renderContext.IndirectGroups.Select(group =>
                config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.HostThenGraphics))
            .ToArray();
        renderContext.DepthIndirectCommandBuffers = renderContext.DepthIndirectGroups.Select(group =>
                config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.HostThenGraphics))
            .ToArray();
        renderContext.IndirectCommandCountBuffers = renderContext.IndirectGroups
            .Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenGraphics)).ToArray();
        renderContext.DepthIndirectCommandCountBuffers = renderContext.DepthIndirectGroups
            .Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenGraphics)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var indirectCommandBuffers = renderContext.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers =
            renderContext.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandBuffers =
            renderContext.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandCountBuffers = renderContext.DepthIndirectCommandCountBuffers
            .Select(graph.GetBufferOrException).ToArray();

        for (var i = 0; i < renderContext.IndirectGroups.Length; i++)
        {
            var group = renderContext.IndirectGroups[i];
            var invokeCount = (uint)group.Length;
            var commandBuffer = indirectCommandBuffers[i];
            var countBuffer = indirectCommandCountBuffers[i];
            countBuffer.Write(invokeCount);
            commandBuffer.Write(group.Select((m, idx) => new VkDrawIndexedIndirectCommand
            {
                instanceCount = 1,
                indexCount = m.IndicesCount,
                firstIndex = m.IndicesStart,
                vertexOffset = (int)m.VertexStart,
                firstInstance = (uint)idx
            }));
        }

        for (var i = 0; i < renderContext.DepthIndirectGroups.Length; i++)
        {
            var group = renderContext.DepthIndirectGroups[i];
            var invokeCount = (uint)group.Length;
            var commandBuffer = depthIndirectCommandBuffers[i];
            var countBuffer = depthIndirectCommandCountBuffers[i];
            countBuffer.Write(invokeCount);
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