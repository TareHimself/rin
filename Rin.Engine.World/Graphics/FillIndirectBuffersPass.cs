using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.World.Graphics;

public class FillIndirectBuffersPass(WorldContext worldContext) : IComputePass
{
    
    private readonly IComputeShader _indirectShader = SGraphicsModule
        .Get()
        .MakeCompute("World/Shaders/Mesh/draw_indirect.slang");
    
    public uint Id { get; set; }
    public bool IsTerminal => true;
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

    private uint[] _meshBuffers = [];
    private uint[] _depthMeshBuffers = [];
    public void Configure(IGraphConfig config)
    {
        config.DependOn(worldContext.InitPassId);
        // _meshBuffers = worldContext.IndirectGroups.Select((group) => config.CreateBuffer<Mesh>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        // _depthMeshBuffers = worldContext.DepthIndirectGroups.Select((group) => config.CreateBuffer<Mesh>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        // worldContext.IndirectCommandBuffers = worldContext.IndirectGroups.Select((group) => config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.Compute)).ToArray();
        // worldContext.DepthIndirectCommandBuffers = worldContext.DepthIndirectGroups.Select((group) => config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.Compute)).ToArray();
        // worldContext.IndirectCommandCountBuffers = worldContext.IndirectGroups.Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
        // worldContext.DepthIndirectCommandCountBuffers = worldContext.DepthIndirectGroups.Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
        _meshBuffers = worldContext.IndirectGroups.Select((group) => config.CreateBuffer<Mesh>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        _depthMeshBuffers = worldContext.DepthIndirectGroups.Select((group) => config.CreateBuffer<Mesh>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        worldContext.IndirectCommandBuffers = worldContext.IndirectGroups.Select((group) => config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        worldContext.DepthIndirectCommandBuffers = worldContext.DepthIndirectGroups.Select((group) => config.CreateBuffer<VkDrawIndexedIndirectCommand>(group.Length, GraphBufferUsage.HostThenCompute)).ToArray();
        worldContext.IndirectCommandCountBuffers = worldContext.IndirectGroups.Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
        worldContext.DepthIndirectCommandCountBuffers = worldContext.DepthIndirectGroups.Select(_ => config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute)).ToArray();
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var indirectCommandBuffers = worldContext.IndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var indirectCommandCountBuffers = worldContext.IndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandBuffers = worldContext.DepthIndirectCommandBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthIndirectCommandCountBuffers = worldContext.DepthIndirectCommandCountBuffers.Select(graph.GetBufferOrException).ToArray();
        var meshBuffers = _meshBuffers.Select(graph.GetBufferOrException).ToArray();
        var depthMeshBuffers = _depthMeshBuffers.Select(graph.GetBufferOrException).ToArray();

        if (_indirectShader.Bind(ctx))
        {
            var param = _indirectShader.Resources["drawCount"];
            for (var i = 0; i < worldContext.IndirectGroups.Length; i++)
            {
                var group = worldContext.IndirectGroups[i];
                var invokeCount = (uint)group.Length;
                var commandBuffer = indirectCommandBuffers[i];
                var meshBuffer = meshBuffers[i];
                var countBuffer = indirectCommandCountBuffers[i];
                countBuffer.Write<uint>(0);
                var set = ctx.AllocateDescriptorSet(_indirectShader, param.Set);
                ctx.BindDescriptorSets(_indirectShader, param.Set, set);
                set.WriteBuffers(param.Binding, new BufferWrite(countBuffer, BufferType.Storage));
                meshBuffer.Write(group.Select((m, idx) => new Mesh
                {
                    Bounds = m.Bounds,
                    IndicesCount = m.IndicesCount,
                    IndicesStart = m.IndicesStart,
                    VertexStart = m.VertexStart,
                    Instance = (uint)idx,
                    MeshId = (uint)idx
                }));
                _indirectShader.Push(ctx,new PushData
                {
                    Meshes = meshBuffer.GetAddress(),
                    InvocationCount = invokeCount,
                    Output = commandBuffer.GetAddress()
                });
                ctx.Invoke(_indirectShader, invokeCount);
            }
            for (var i = 0; i < worldContext.DepthIndirectGroups.Length; i++)
            {
                var group = worldContext.DepthIndirectGroups[i];
                var invokeCount = (uint)group.Length;
                var commandBuffer = depthIndirectCommandBuffers[i];
                var meshBuffer = depthMeshBuffers[i];
                var countBuffer = depthIndirectCommandCountBuffers[i];
                countBuffer.Write<uint>(0);
                var set = ctx.AllocateDescriptorSet(_indirectShader, param.Set);
                ctx.BindDescriptorSets(_indirectShader, param.Set, set);
                set.WriteBuffers(param.Binding, new BufferWrite(countBuffer, BufferType.Storage));
                meshBuffer.Write(group.Select((m, idx) => new Mesh
                {
                    Bounds = m.Bounds,
                    IndicesCount = m.IndicesCount,
                    IndicesStart = m.IndicesStart,
                    VertexStart = m.VertexStart,
                    Instance = (uint)idx,
                    MeshId = (uint)idx
                }));
                _indirectShader.Push(ctx,new PushData
                {
                    Meshes = meshBuffer.GetAddress(),
                    InvocationCount = invokeCount,
                    Output = commandBuffer.GetAddress()
                });
                ctx.Invoke(_indirectShader, invokeCount);
            }
        }
    }

    struct Mesh
    {
        public required Bounds3D Bounds;
        public required uint IndicesCount;
        public required uint IndicesStart;
        public required uint VertexStart;
        public required uint Instance;
        public required uint MeshId;
    }
    
    struct PushData {
        public required ulong Meshes;
        public required uint InvocationCount;
        public required ulong Output;
    };
}