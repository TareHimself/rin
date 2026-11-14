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

        view.IndirectCommandBuffers = CreatePerBatch<DrawIndexedIndirectCommand>(config, sceneFrame.ColorBatches);
        view.DepthIndirectCommandBuffers =
            CreatePerBatch<DrawIndexedIndirectCommand>(config, sceneFrame.DepthBatches);
        view.IndirectCommandCountBuffers = CreateCounters(config, sceneFrame.ColorBatches.Count);
        view.DepthIndirectCommandCountBuffers = CreateCounters(config, sceneFrame.DepthBatches.Count);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (Shader.Bind(ctx) is not { } bindContext) return;

        var sceneFrame = view.SceneFrame;
        var cullingResults = graph.GetBufferOrException(cullingPass.OutputBufferId).ToDeviceSpan<uint>();
        DispatchBatches(graph, bindContext, cullingResults, sceneFrame.ColorBatches,
            sceneFrame.MeshRecordBufferIds, view.IndirectCommandBuffers, view.IndirectCommandCountBuffers);
        DispatchBatches(graph, bindContext, cullingResults, sceneFrame.DepthBatches,
            sceneFrame.DepthMeshRecordBufferIds, view.DepthIndirectCommandBuffers,
            view.DepthIndirectCommandCountBuffers);
    }

    private static uint[] CreatePerBatch<T>(IGraphConfig config, MeshBatches batches) where T : unmanaged
    {
        var ids = new uint[batches.Count];
        for (var i = 0; i < ids.Length; i++)
            ids[i] = config.CreateBuffer<T>(batches[i].Length, GraphBufferUsage.Compute);
        return ids;
    }

    private static uint[] CreateCounters(IGraphConfig config, int count)
    {
        var ids = new uint[count];
        for (var i = 0; i < count; i++) ids[i] = config.CreateBuffer<uint>(GraphBufferUsage.HostThenCompute);
        return ids;
    }

    private static void DispatchBatches(ICompiledGraph graph, IComputeBindContext bindContext,
        DeviceSpan<uint> cullingResults, MeshBatches batches,
        uint[] meshRecordBufferIds, uint[] commandBufferIds, uint[] countBufferIds)
    {
        for (var i = 0; i < batches.Count; i++)
        {
            var meshRecords = graph.GetBufferOrException(meshRecordBufferIds[i]);
            var commandBuffer = graph.GetBufferOrException(commandBufferIds[i]);
            var countBuffer = graph.GetBufferOrException(countBufferIds[i]);

            countBuffer.WriteSingle<uint>(0);

            var invokeCount = (uint)batches[i].Length;
            bindContext
                .Reset()
                .Push(new DrawIndirectShader.PushConstants
                {
                    CullingResults = cullingResults,
                    Meshes = meshRecords.ToDeviceSpan<IndirectMeshRecord>(),
                    InvocationCount = invokeCount,
                    Output = commandBuffer.ToDeviceSpan<DrawIndexedIndirectCommand>(),
                    DrawCount = countBuffer.ToDeviceSpan<uint>()
                })
                .Invoke(invokeCount);
        }
    }
}