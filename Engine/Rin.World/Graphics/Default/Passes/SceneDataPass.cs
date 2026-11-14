using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Uploads the per-mesh data every view draws from: material data for the color and depth passes, and the
///     mesh records the indirect-draw compute reads. Runs after skinning so skinned vertex buffer views are final.
/// </summary>
public class SceneDataPass(DefaultSceneFrame sceneFrame) : IPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        if (sceneFrame.SkinningPassId != 0) config.DependOn(sceneFrame.SkinningPassId);

        sceneFrame.ColorMaterialBufferIds =
            CreateMaterialBuffers(config, sceneFrame.ColorBatches, material => material.ColorPass);
        sceneFrame.DepthMaterialBufferIds =
            CreateMaterialBuffers(config, sceneFrame.DepthBatches, material => material.DepthPass);
        sceneFrame.MeshRecordBufferIds = CreateMeshRecordBuffers(config, sceneFrame.ColorBatches);
        sceneFrame.DepthMeshRecordBufferIds = CreateMeshRecordBuffers(config, sceneFrame.DepthBatches);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        WriteMaterialData(graph, sceneFrame.ColorBatches, sceneFrame.ColorMaterialBufferIds,
            material => material.ColorPass);
        WriteMaterialData(graph, sceneFrame.DepthBatches, sceneFrame.DepthMaterialBufferIds,
            material => material.DepthPass);
        WriteMeshRecords(graph, sceneFrame.ColorBatches, sceneFrame.MeshRecordBufferIds);
        WriteMeshRecords(graph, sceneFrame.DepthBatches, sceneFrame.DepthMeshRecordBufferIds);
    }

    private uint[] CreateMaterialBuffers(IGraphConfig config, MeshBatches batches,
        Func<IMeshMaterial, IMaterialPass> selectPass)
    {
        var meshes = sceneFrame.ProcessedMeshes;
        var ids = new uint[batches.Count];
        for (var i = 0; i < ids.Length; i++)
        {
            var batch = batches[i];
            var size = selectPass(meshes[batch[0]].Material).GetRequiredMemory() * (ulong)batch.Length;
            ids[i] = size > 0 ? config.CreateBuffer(size, GraphBufferUsage.HostThenGraphics) : 0;
        }

        return ids;
    }

    private static uint[] CreateMeshRecordBuffers(IGraphConfig config, MeshBatches batches)
    {
        var ids = new uint[batches.Count];
        for (var i = 0; i < ids.Length; i++)
            ids[i] = config.CreateBuffer<IndirectMeshRecord>(batches[i].Length, GraphBufferUsage.HostThenCompute);
        return ids;
    }

    private void WriteMaterialData(ICompiledGraph graph, MeshBatches batches, uint[] bufferIds,
        Func<IMeshMaterial, IMaterialPass> selectPass)
    {
        var meshes = sceneFrame.ProcessedMeshes;
        for (var i = 0; i < batches.Count; i++)
        {
            var bufferId = bufferIds[i];
            if (bufferId == 0) continue;

            var batch = batches[i];
            var dataSize = (int)selectPass(meshes[batch[0]].Material).GetRequiredMemory();
            using var data = new PooledMemory<byte>(dataSize * batch.Length);
            var span = data.AsSpan();
            for (var j = 0; j < batch.Length; j++)
            {
                ref readonly var mesh = ref meshes[batch[j]];
                selectPass(mesh.Material).Write(span.Slice(j * dataSize, dataSize), mesh);
            }

            graph.GetBufferOrException(bufferId).Write(data);
        }
    }

    private void WriteMeshRecords(ICompiledGraph graph, MeshBatches batches, uint[] bufferIds)
    {
        var meshes = sceneFrame.ProcessedMeshes;
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            using var records = new PooledMemory<IndirectMeshRecord>(batch.Length);
            for (var j = 0; j < batch.Length; j++)
            {
                ref readonly var mesh = ref meshes[batch[j]];
                records[j] = new IndirectMeshRecord
                {
                    IndicesCount = mesh.Surface.IndicesCount,
                    IndicesStart = mesh.Surface.IndicesStart,
                    VertexStart = mesh.Surface.VertexStart,
                    Instance = (uint)j,
                    MeshIndex = mesh.AbsoluteMeshIndex
                };
            }

            graph.GetBufferOrException(bufferIds[i]).Write(records);
        }
    }
}
