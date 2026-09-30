using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

public partial class FillIndirectBuffersPass(CullingPass cullingPass, DefaultWorldViewData view)
    : IComputePass
{
    [ComputeShader<DrawIndirectShader>]
    private partial IComputeShader Shader { get; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        var sceneFrame = view.SceneFrame;
        config.ReadBuffer(cullingPass.OutputBufferId, GraphBufferUsage.Compute);
        foreach (var id in sceneFrame.MeshRecordBufferIds) config.ReadBuffer(id, GraphBufferUsage.Compute);
        foreach (var id in sceneFrame.DepthMeshRecordBufferIds) config.ReadBuffer(id, GraphBufferUsage.Compute);

        view.IndirectCommandBuffers = CreatePerGroup<DrawIndexedIndirectCommand>(config, sceneFrame.IndirectGroups);
        view.DepthIndirectCommandBuffers =
            CreatePerGroup<DrawIndexedIndirectCommand>(config, sceneFrame.DepthIndirectGroups);
        view.IndirectCommandCountBuffers = CreateCounters(config, sceneFrame.IndirectGroups.Count);
        view.DepthIndirectCommandCountBuffers = CreateCounters(config, sceneFrame.DepthIndirectGroups.Count);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (Shader.Bind(ctx) is not { } bindContext) return;

        var sceneFrame = view.SceneFrame;
        var cullingBufferAddress = graph.GetBufferOrException(cullingPass.OutputBufferId).GetAddress();
        DispatchGroups(graph, bindContext, cullingBufferAddress, sceneFrame.IndirectGroups,
            sceneFrame.MeshRecordBufferIds, view.IndirectCommandBuffers, view.IndirectCommandCountBuffers);
        DispatchGroups(graph, bindContext, cullingBufferAddress, sceneFrame.DepthIndirectGroups,
            sceneFrame.DepthMeshRecordBufferIds, view.DepthIndirectCommandBuffers,
            view.DepthIndirectCommandCountBuffers);
    }

    private static uint[] CreatePerGroup<T>(IGraphConfig config,
        IReadOnlyDictionary<DefaultSceneFrame.BatchKey, List<ProcessedMesh>> groups) where T : unmanaged
    {
        var ids = new uint[groups.Count];
        var i = 0;
        foreach (var group in groups.Values) ids[i++] = config.CreateBuffer<T>(group.Count, GraphBufferUsage.Compute);
        return ids;
    }

    private static uint[] CreateCounters(IGraphConfig config, int count)
    {
        var ids = new uint[count];
        for (var i = 0; i < count; i++) ids[i] = config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute);
        return ids;
    }

    private static void DispatchGroups(ICompiledGraph graph, IComputeBindContext bindContext,
        ulong cullingBufferAddress, IReadOnlyDictionary<DefaultSceneFrame.BatchKey, List<ProcessedMesh>> groups,
        uint[] meshRecordBufferIds, uint[] commandBufferIds, uint[] countBufferIds)
    {
        var i = 0;
        foreach (var group in groups.Values)
        {
            var meshRecords = graph.GetBufferOrException(meshRecordBufferIds[i]);
            var commandBuffer = graph.GetBufferOrException(commandBufferIds[i]);
            var countBuffer = graph.GetBufferOrException(countBufferIds[i]);
            i++;

            countBuffer.WriteSingle<uint>(0);

            var invokeCount = (uint)group.Count;
            bindContext
                .Reset() // We have to reset because we write shader data
                .Push(new DrawIndirectShader.PushConstants
                {
                    CullingResults = new BufferRef<uint>(cullingBufferAddress),
                    Meshes = new BufferRef<IndirectMeshRecord>(meshRecords.GetAddress()),
                    InvocationCount = invokeCount,
                    Output = new BufferRef<DrawIndexedIndirectCommand>(commandBuffer.GetAddress()),
                    DrawCount = new BufferRef<uint>(countBuffer.GetAddress())
                })
                .Invoke(invokeCount);
        }
    }
}