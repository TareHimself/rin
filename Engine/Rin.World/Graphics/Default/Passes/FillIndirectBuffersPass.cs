using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics.Default.Passes;

public partial class FillIndirectBuffersPass(CullingPass cullingPass, DefaultWorldCollectedData collectedData) : IComputePass
{
    [ComputeShader("Shaders/World/Mesh/Compute/draw_indirect.slang")]
    private partial IComputeShader Shader { get; }

    private uint[] _bufferIds = [];
    private uint[] _depthMeshBuffers = [];

    private uint _frustumBuffer;

    private uint[] _meshBuffers = [];

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        config.ReadBuffer(collectedData.BoundsBufferId, GraphBufferUsage.Compute);
        config.ReadBuffer(cullingPass.OutputBufferId, GraphBufferUsage.Compute);

        _meshBuffers = collectedData.IndirectGroups.Values
            .Select(group => config.CreateBuffer<Mesh>(group.Count, GraphBufferUsage.HostThenCompute)).ToArray();
        _depthMeshBuffers = collectedData.DepthIndirectGroups
            .Values.Select(group => config.CreateBuffer<Mesh>(group.Count, GraphBufferUsage.HostThenCompute)).ToArray();
        collectedData.IndirectCommandBuffers = collectedData.IndirectGroups.Values.Select(group =>
            config.CreateBuffer<DrawIndexedIndirectCommand>(group.Count, GraphBufferUsage.Compute)).ToArray();
        collectedData.DepthIndirectCommandBuffers = collectedData.DepthIndirectGroups.Values.Select(group =>
            config.CreateBuffer<DrawIndexedIndirectCommand>(group.Count, GraphBufferUsage.Compute)).ToArray();
        collectedData.IndirectCommandCountBuffers = collectedData.IndirectGroups
            .Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
        collectedData.DepthIndirectCommandCountBuffers = collectedData.DepthIndirectGroups
            .Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var cullingBuffer = graph.GetBufferOrException(cullingPass.OutputBufferId);
        var cullingBufferAddress = cullingBuffer.GetAddress();
        var indirectCommandBuffers = collectedData.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers =
            collectedData.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandBuffers =
            collectedData.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandCountBuffers = collectedData.DepthIndirectCommandCountBuffers
            .Select(graph.GetBufferOrException).ToArray();
        var meshBuffers = _meshBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthMeshBuffers = _depthMeshBuffers.Select(graph.GetBufferOrException).ToArray();
        
        
        if (Shader.Bind(ctx) is { } bindContext)
        {
            
            var i = 0;
            foreach (var group in collectedData.IndirectGroups.Values)
            {
                bindContext.Reset(); // We have to reset because we write shader data
                var invokeCount = (uint)group.Count;
                var commandBuffer = indirectCommandBuffers[i];
                var meshBuffer = meshBuffers[i];
                var countBuffer = indirectCommandCountBuffers[i];
                countBuffer.WriteSingle<uint>(0);
                meshBuffer.Write(group.Select((m, idx) => new Mesh
                {
                    IndicesCount = m.Surface.IndicesCount,
                    IndicesStart = m.Surface.IndicesStart,
                    VertexStart = m.Surface.VertexStart,
                    Instance = (uint)idx,
                    MeshIndex = m.AbsoluteMeshIndex
                }).ToArray());
                bindContext
                    .Push(new PushData
                    {
                        CullingBufferAddress = cullingBufferAddress,
                        Meshes = meshBuffer.GetAddress(),
                        InvocationCount = invokeCount,
                        Output = commandBuffer.GetAddress(),
                        DrawCount = countBuffer.GetAddress()
                    })
                    .Invoke(invokeCount);
                i++;
            }


            i = 0;
            foreach (var group in collectedData.DepthIndirectGroups.Values)
            {
                bindContext.Reset(); // We have to reset because we write shader data
                var invokeCount = (uint)group.Count;
                var commandBuffer = depthIndirectCommandBuffers[i];
                var meshBuffer = depthMeshBuffers[i];
                var countBuffer = depthIndirectCommandCountBuffers[i];
                countBuffer.WriteSingle<uint>(0);
                meshBuffer.Write(group.Select((m, idx) => new Mesh
                {
                    IndicesCount = m.Surface.IndicesCount,
                    IndicesStart = m.Surface.IndicesStart,
                    VertexStart = m.Surface.VertexStart,
                    Instance = (uint)idx,
                    MeshIndex = m.AbsoluteMeshIndex
                }).ToArray());
                bindContext
                    .Push(new PushData
                    {
                        CullingBufferAddress = cullingBufferAddress,
                        Meshes = meshBuffer.GetAddress(),
                        InvocationCount = invokeCount,
                        Output = commandBuffer.GetAddress(),
                        DrawCount = countBuffer.GetAddress()
                    })
                    .Invoke(invokeCount);
                i++;
            }
        }
    }
    [NoReorder]
    private struct Mesh
    {
        public required uint IndicesCount;
        public required uint IndicesStart;
        public required uint VertexStart;
        public required uint Instance;
        public required int MeshIndex;
    }

    [NoReorder]
    private struct PushData
    {
        public required ulong CullingBufferAddress;
        public required ulong Meshes;
        public required uint InvocationCount;
        public required ulong Output;
        public required ulong DrawCount;
    }
}