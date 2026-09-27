using System.Numerics;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;
using Rin.World.Graphics.Default.Passes;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default;

/// <summary>
///     The view-independent half of a world's frame inside one graph: processed meshes, batches, bounds, skinning
///     and per-mesh draw data. Every view of the same <see cref="DefaultWorldSnapshot" /> in a graph shares it.
/// </summary>
public class DefaultSceneFrame : IDisposable
{
    private readonly Dictionary<IMesh, (uint Vertices, uint Indices)> _meshBufferIds = [];

    public DefaultSceneFrame(DefaultWorldSnapshot snapshot, IGraphBuilder builder)
    {
        Snapshot = snapshot;
        ProcessMeshes(builder);
        SkinningMatrices = ComputeSkinningMatrices(snapshot.SkinnedGeometry);

        builder.AddPass(new InitSceneResourcesPass(this));
        if (ProcessedSkinnedMeshCount > 0)
        {
            SkinningPassId = builder.AddPass(new SkinningPass(this));
            builder.AddPass(new BoundsUpdatePass(this));
        }

        builder.AddPass(new SceneDataPass(this));
    }

    public DefaultWorldSnapshot Snapshot { get; }
    public SkinnedMeshInfo[] SkinnedGeometry => Snapshot.SkinnedGeometry;
    public IReadOnlyList<ProcessedMesh> ProcessedMeshes { get; private set; } = [];

    /// <summary>Per <see cref="SkinnedGeometry" /> entry.</summary>
    public PooledMemory<Matrix4x4>[] SkinningMatrices { get; }
    public int TotalMeshCount => ProcessedMeshes.Count;
    public int SkinnedMeshStartIndex { get; private set; }
    public int ProcessedSkinnedMeshCount => ProcessedMeshes.Count - SkinnedMeshStartIndex;
    public int SkinnedSurfaceCount { get; private set; }
    public long SkinnedVertexCount { get; private set; }

    public IReadOnlyDictionary<BatchKey, List<ProcessedMesh>> IndirectGroups { get; private set; } =
        new Dictionary<BatchKey, List<ProcessedMesh>>();

    public IReadOnlyDictionary<BatchKey, List<ProcessedMesh>> DepthIndirectGroups { get; private set; } =
        new Dictionary<BatchKey, List<ProcessedMesh>>();

    public uint SkinningPassId { get; private set; }
    public uint BoundsBufferId { get; set; }
    public uint SkinningOutputBufferId { get; set; }

    /// <summary>Per <see cref="IndirectGroups" /> entry; 0 when the group's material needs no data.</summary>
    public uint[] ColorMaterialBufferIds { get; set; } = [];

    /// <summary>Per <see cref="DepthIndirectGroups" /> entry; 0 when the group's material needs no data.</summary>
    public uint[] DepthMaterialBufferIds { get; set; } = [];

    public uint[] MeshRecordBufferIds { get; set; } = [];
    public uint[] DepthMeshRecordBufferIds { get; set; } = [];

    public void Dispose()
    {
        foreach (var matrices in SkinningMatrices) matrices.Dispose();
    }

    private static PooledMemory<Matrix4x4>[] ComputeSkinningMatrices(SkinnedMeshInfo[] skinnedGeometry)
    {
        var matrices = new PooledMemory<Matrix4x4>[skinnedGeometry.Length];
        for (var i = 0; i < skinnedGeometry.Length; i++)
        {
            var mesh = skinnedGeometry[i];
            using var globalPose = mesh.Skeleton.ResolvePosePooled(mesh.Pose);
            matrices[i] = new PooledMemory<Matrix4x4>(globalPose.Count);
            mesh.Skeleton.ComputeSkinningMatrices(globalPose.AsSpan(), matrices[i].AsSpan());
        }

        return matrices;
    }

    public void DeclareDrawResources(IGraphConfig config, IReadOnlyDictionary<BatchKey, List<ProcessedMesh>> groups,
        Func<IMeshMaterial, IMaterialPass> selectPass)
    {
        HashSet<uint> read = [];
        HashSet<IMeshMaterial> declaredMaterials = [];
        foreach (var group in groups.Values)
        foreach (var mesh in group)
        {
            if (declaredMaterials.Add(mesh.Material)) selectPass(mesh.Material).DeclareResources(config, mesh);
            if (read.Add(mesh.IndexBufferId)) config.ReadBuffer(mesh.IndexBufferId, GraphBufferUsage.Graphics);
            if (mesh.AbsoluteMeshIndex < SkinnedMeshStartIndex && read.Add(mesh.SourceVertexBufferId))
                config.ReadBuffer(mesh.SourceVertexBufferId, GraphBufferUsage.Graphics);
        }
    }

    private bool TryUseMesh(IGraphBuilder builder, IMesh mesh, out (uint Vertices, uint Indices) ids)
    {
        if (_meshBufferIds.TryGetValue(mesh, out ids)) return true;

        ids = (builder.AddExternalBuffer(mesh.GetVertices()), builder.AddExternalBuffer(mesh.GetIndices()));
        if (ids.Vertices == 0 || ids.Indices == 0) return false;

        _meshBufferIds.Add(mesh, ids);
        return true;
    }

    private void ProcessMeshes(IGraphBuilder builder)
    {
        var meshes = new List<ProcessedMesh>();

        var staticGeometry = Snapshot.StaticGeometry;
        for (var i = 0; i < staticGeometry.Length; i++)
        {
            var mesh = staticGeometry[i];
            if (!TryUseMesh(builder, mesh.Mesh, out var staticIds)) continue;
            foreach (var surfaceIndex in mesh.SurfaceIndices)
                meshes.Add(new ProcessedMesh
                {
                    AbsoluteMeshIndex = meshes.Count,
                    GeometryId = i,
                    SurfaceIndex = surfaceIndex,
                    Surface = mesh.Mesh.GetSurface(surfaceIndex),
                    Transform = mesh.Transform,
                    IndexBuffer = mesh.Mesh.GetIndices(),
                    IndexBufferId = staticIds.Indices,
                    SourceVertexBufferId = staticIds.Vertices,
                    VertexBuffer = mesh.Mesh.GetVertices(surfaceIndex),
                    Material = mesh.Materials[surfaceIndex]
                });
        }

        SkinnedMeshStartIndex = meshes.Count;
        ulong skinnedOutputOffset = 0;
        var skinnedGeometry = Snapshot.SkinnedGeometry;
        for (var i = 0; i < skinnedGeometry.Length; i++)
        {
            var mesh = skinnedGeometry[i];
            if (!TryUseMesh(builder, mesh.Mesh, out var skinnedIds)) continue;

            foreach (var surfaceIndex in mesh.SurfaceIndices)
            {
                var surface = mesh.Mesh.GetSurface(surfaceIndex);
                var size = mesh.Mesh.GetVertexCount(surfaceIndex) * Utils.ByteSizeOf<Vertex>();
                meshes.Add(new ProcessedMesh
                {
                    AbsoluteMeshIndex = meshes.Count,
                    GeometryId = i,
                    SurfaceIndex = surfaceIndex,
                    Surface = surface,
                    Transform = mesh.Transform,
                    IndexBuffer = mesh.Mesh.GetIndices(),
                    IndexBufferId = skinnedIds.Indices,
                    SourceVertexBufferId = skinnedIds.Vertices,
                    VertexBuffer = new DeviceBufferView(ResourceHandle.InvalidBuffer, skinnedOutputOffset, size),
                    Material = mesh.Materials[surfaceIndex]
                });
                skinnedOutputOffset += size;
                SkinnedSurfaceCount++;
                SkinnedVertexCount += surface.VertexCount;
            }
        }

        ProcessedMeshes = meshes;

        var colorGroups = new Dictionary<BatchKey, List<ProcessedMesh>>();
        var depthGroups = new Dictionary<BatchKey, List<ProcessedMesh>>();
        foreach (var mesh in meshes)
        {
            var colorKey = new BatchKey(mesh.IndexBuffer, mesh.Material.GetColorIdentity());
            var depthKey = new BatchKey(mesh.IndexBuffer, mesh.Material.GetDepthIdentity());
            if (!colorGroups.TryGetValue(colorKey, out var colorGroup)) colorGroups[colorKey] = colorGroup = [];
            if (!depthGroups.TryGetValue(depthKey, out var depthGroup)) depthGroups[depthKey] = depthGroup = [];
            colorGroup.Add(mesh);
            depthGroup.Add(mesh);
        }

        IndirectGroups = colorGroups;
        DepthIndirectGroups = depthGroups;
    }

    public readonly record struct BatchKey(DeviceBufferView IndexBuffer, MaterialIdentity Material);
}
