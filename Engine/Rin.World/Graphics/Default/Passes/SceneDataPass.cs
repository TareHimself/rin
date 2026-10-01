using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Uploads the per-mesh data every view draws from: material data for the color and depth passes, and the
///     mesh records the indirect-draw compute reads. Runs after skinning so skinned vertex addresses are final.
/// </summary>
public class SceneDataPass(DefaultSceneFrame sceneFrame) : IPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        if (sceneFrame.SkinningPassId != 0) config.DependOn(sceneFrame.SkinningPassId);

        sceneFrame.ColorMaterialBufferIds =
            CreateMaterialBuffers(config, sceneFrame.IndirectGroups, material => material.ColorPass);
        sceneFrame.DepthMaterialBufferIds =
            CreateMaterialBuffers(config, sceneFrame.DepthIndirectGroups, material => material.DepthPass);
        sceneFrame.MeshRecordBufferIds = CreateMeshRecordBuffers(config, sceneFrame.IndirectGroups);
        sceneFrame.DepthMeshRecordBufferIds = CreateMeshRecordBuffers(config, sceneFrame.DepthIndirectGroups);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        WriteMaterialData(graph, sceneFrame.IndirectGroups, sceneFrame.ColorMaterialBufferIds,
            material => material.ColorPass);
        WriteMaterialData(graph, sceneFrame.DepthIndirectGroups, sceneFrame.DepthMaterialBufferIds,
            material => material.DepthPass);
        WriteMeshRecords(graph, sceneFrame.IndirectGroups, sceneFrame.MeshRecordBufferIds);
        WriteMeshRecords(graph, sceneFrame.DepthIndirectGroups, sceneFrame.DepthMeshRecordBufferIds);
    }

    private static uint[] CreateMaterialBuffers(IGraphConfig config,
        IReadOnlyDictionary<DefaultSceneFrame.BatchKey, List<ProcessedMesh>> groups,
        Func<IMeshMaterial, IMaterialPass> selectPass)
    {
        var ids = new uint[groups.Count];
        var i = 0;
        foreach (var group in groups.Values)
        {
            var size = selectPass(group[0].Material).GetRequiredMemory() * (ulong)group.Count;
            ids[i++] = size > 0 ? config.CreateBuffer(size, GraphBufferUsage.HostThenGraphics) : 0;
        }

        return ids;
    }

    private static uint[] CreateMeshRecordBuffers(IGraphConfig config,
        IReadOnlyDictionary<DefaultSceneFrame.BatchKey, List<ProcessedMesh>> groups)
    {
        var ids = new uint[groups.Count];
        var i = 0;
        foreach (var group in groups.Values)
            ids[i++] = config.CreateBuffer<IndirectMeshRecord>(group.Count, GraphBufferUsage.HostThenCompute);
        return ids;
    }

    private static void WriteMaterialData(ICompiledGraph graph,
        IReadOnlyDictionary<DefaultSceneFrame.BatchKey, List<ProcessedMesh>> groups, uint[] bufferIds,
        Func<IMeshMaterial, IMaterialPass> selectPass)
    {
        var i = 0;
        foreach (var group in groups.Values)
        {
            var bufferId = bufferIds[i++];
            if (bufferId == 0) continue;

            var dataSize = (int)selectPass(group[0].Material).GetRequiredMemory();
            using var data = new PooledMemory<byte>(dataSize * group.Count);
            var span = data.AsSpan();
            for (var j = 0; j < group.Count; j++)
                selectPass(group[j].Material).Write(span.Slice(j * dataSize, dataSize), group[j]);
            graph.GetBufferOrException(bufferId).Write(data);
        }
    }

    private static void WriteMeshRecords(ICompiledGraph graph,
        IReadOnlyDictionary<DefaultSceneFrame.BatchKey, List<ProcessedMesh>> groups, uint[] bufferIds)
    {
        var i = 0;
        foreach (var group in groups.Values)
        {
            using var records = new PooledMemory<IndirectMeshRecord>(group.Count);
            for (var j = 0; j < group.Count; j++)
            {
                var mesh = group[j];
                records[j] = new IndirectMeshRecord
                {
                    IndicesCount = mesh.Surface.IndicesCount,
                    IndicesStart = mesh.Surface.IndicesStart,
                    VertexStart = mesh.Surface.VertexStart,
                    Instance = (uint)j,
                    MeshIndex = mesh.AbsoluteMeshIndex
                };
            }

            graph.GetBufferOrException(bufferIds[i++]).Write(records);
        }
    }
}
