using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics.Default.Passes;

public class  FillIndirectBuffersPass(CullingPass cullingPass, DefaultWorldRenderContext renderContext) : IComputePass
{
    private readonly IComputeShader _shader = SGraphicsModule
        .Get()
        .MakeCompute("World/Shaders/Mesh/Compute/draw_indirect.slang");

    private uint[] _bufferIds = [];
    private uint[] _depthMeshBuffers = [];

    private uint _frustumBuffer;

    private uint[] _meshBuffers = [];

    public uint Id { get; set; }
    public bool IsTerminal => true;

    public void Configure(IGraphConfig config)
    {
        config.DependOn(renderContext.InitPassId);
        config.ReadBuffer(renderContext.BoundsBufferId, GraphBufferUsage.Compute);
        config.ReadBuffer(cullingPass.OutputBufferId, GraphBufferUsage.Compute);

        _meshBuffers = renderContext.IndirectGroups
            .Select(group => config.CreateBuffer<Mesh>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        _depthMeshBuffers = renderContext.DepthIndirectGroups
            .Select(group => config.CreateBuffer<Mesh>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        renderContext.IndirectCommandBuffers = renderContext.IndirectGroups.Select(group =>
            config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.Compute)).ToArray();
        renderContext.DepthIndirectCommandBuffers = renderContext.DepthIndirectGroups.Select(group =>
            config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.Compute)).ToArray();
        renderContext.IndirectCommandCountBuffers = renderContext.IndirectGroups
            .Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
        renderContext.DepthIndirectCommandCountBuffers = renderContext.DepthIndirectGroups
            .Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var cullingBuffer = graph.GetBufferOrException(cullingPass.OutputBufferId);
        var cullingBufferAddress = cullingBuffer.GetAddress();
        var indirectCommandBuffers = renderContext.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers =
            renderContext.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandBuffers =
            renderContext.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandCountBuffers = renderContext.DepthIndirectCommandCountBuffers
            .Select(graph.GetBufferOrException).ToArray();
        var meshBuffers = _meshBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthMeshBuffers = _depthMeshBuffers.Select(graph.GetBufferOrException).ToArray();

        if (_shader.Bind(ctx) is {} bindContext)
        {
            for (var i = 0; i < renderContext.IndirectGroups.Length; i++)
            {
                bindContext.Reset(); // We have to reset because we write shader data
                var group = renderContext.IndirectGroups[i];
                var invokeCount = (uint)group.Length;
                var commandBuffer = indirectCommandBuffers[i];
                var meshBuffer = meshBuffers[i];
                var countBuffer = indirectCommandCountBuffers[i];
                countBuffer.Write<uint>(0);
                bindContext.WriteBuffer("drawCount", countBuffer);
                meshBuffer.Write(group.Select((m, idx) => new Mesh
                {
                    IndicesCount = m.IndicesCount,
                    IndicesStart = m.IndicesStart,
                    VertexStart = m.VertexStart,
                    Instance = (uint)idx,
                    MeshIndex = m.Id
                }));
                bindContext
                    .Push(new PushData
                    {
                        CullingBufferAddress = cullingBufferAddress,
                        Meshes = meshBuffer.GetAddress(),
                        InvocationCount = invokeCount,
                        Output = commandBuffer.GetAddress()
                    })
                    .Invoke(invokeCount);
            }

            for (var i = 0; i < renderContext.DepthIndirectGroups.Length; i++)
            {
                bindContext.Reset(); // We have to reset because we write shader data
                var group = renderContext.DepthIndirectGroups[i];
                var invokeCount = (uint)group.Length;
                var commandBuffer = depthIndirectCommandBuffers[i];
                var meshBuffer = depthMeshBuffers[i];
                var countBuffer = depthIndirectCommandCountBuffers[i];
                countBuffer.Write<uint>(0);
                bindContext.WriteBuffer("drawCount", countBuffer);
                meshBuffer.Write(group.Select((m, idx) => new Mesh
                {
                    IndicesCount = m.IndicesCount,
                    IndicesStart = m.IndicesStart,
                    VertexStart = m.VertexStart,
                    Instance = (uint)idx,
                    MeshIndex = m.Id
                }));
                bindContext
                    .Push(new PushData
                    {
                        CullingBufferAddress = cullingBufferAddress,
                        Meshes = meshBuffer.GetAddress(),
                        InvocationCount = invokeCount,
                        Output = commandBuffer.GetAddress()
                    })
                    .Invoke(invokeCount);
            }
        }
    }

    private struct Mesh
    {
        public required uint IndicesCount;
        public required uint IndicesStart;
        public required uint VertexStart;
        public required uint Instance;
        public required int MeshIndex;
    }


    private struct PushData
    {
        public required ulong CullingBufferAddress;
        public required ulong Meshes;
        public required uint InvocationCount;
        public required ulong Output;
    }
}