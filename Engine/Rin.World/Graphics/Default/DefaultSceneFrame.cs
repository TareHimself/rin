using System.Numerics;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;
using Rin.World.Graphics.Default.Passes;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default;

/// <summary>
///     The view-independent half of a world's frame inside one graph: processed meshes, batches, bounds, skinning
///     and per-mesh draw data. Every view of the same <see cref="DefaultWorldSnapshot" /> in a graph shares it.
/// </summary>
public class DefaultSceneFrame : IDisposable
{
    private readonly Dictionary<IMesh, (uint Vertices, uint Indices)> _meshBufferIds = [];
    private PooledMemory<ProcessedMesh> _meshes;
    private bool _disposed;

    public DefaultSceneFrame(DefaultWorldSnapshot snapshot, IGraphBuilder builder)
    {
        Snapshot = snapshot;
        snapshot.Acquire();
        ProcessMeshes(builder);
        SkinningMatrices = ComputeSkinningMatrices(snapshot);

        AddEnvironmentFilterPasses(builder);
        builder.AddPass(new InitSceneResourcesPass(this));
        if (ProcessedSkinnedMeshCount > 0)
        {
            SkinningPassId = builder.AddPass(new SkinningPass(this));
            builder.AddPass(new BoundsUpdatePass(this));
        }

        builder.AddPass(new SceneDataPass(this));
    }

    public DefaultWorldSnapshot Snapshot { get; }

    // Each map is blurred once, by the first frame that uses it.
    private void AddEnvironmentFilterPasses(IGraphBuilder builder)
    {
        var environments = Snapshot.Environments;
        for (var i = 0; i < environments.Length; i++)
        {
            var map = environments[i].Map;
            if (map.IsFiltered || IsEarlierMap(environments, i)) continue;
            builder.AddPass(new EnvironmentFilterPass(map));
        }
    }

    private static bool IsEarlierMap(ReadOnlySpan<EnvironmentVolume> environments, int index)
    {
        for (var earlier = 0; earlier < index; earlier++)
            if (ReferenceEquals(environments[earlier].Map, environments[index].Map))
                return true;
        return false;
    }
    public ReadOnlySpan<SkinnedMeshInfo> SkinnedGeometry => Snapshot.SkinnedGeometry;
    /// <summary>
    ///     Every surface the frame draws, static ones first. Batches index into it.
    /// </summary>
    public ReadOnlySpan<ProcessedMesh> ProcessedMeshes => _meshes.AsSpan();

    /// <summary>
    ///     Per <see cref="SkinnedGeometry" /> entry.
    /// </summary>
    public PooledMemory<Matrix4x4>[] SkinningMatrices { get; }
    public int TotalMeshCount => _meshes.Count;
    public int SkinnedMeshStartIndex { get; private set; }
    public int ProcessedSkinnedMeshCount => _meshes.Count - SkinnedMeshStartIndex;
    public int SkinnedSurfaceCount { get; private set; }
    public long SkinnedVertexCount { get; private set; }

    public MeshBatches ColorBatches { get; private set; } = null!;

    public MeshBatches DepthBatches { get; private set; } = null!;

    public uint SkinningPassId { get; private set; }
    public uint BoundsBufferId { get; set; }
    public uint SkinningOutputBufferId { get; set; }

    /// <summary>
    ///     Per <see cref="ColorBatches" /> entry; 0 when the batch's material needs no data.
    /// </summary>
    public uint[] ColorMaterialBufferIds { get; set; } = [];

    /// <summary>
    ///     Per <see cref="DepthBatches" /> entry; 0 when the batch's material needs no data.
    /// </summary>
    public uint[] DepthMaterialBufferIds { get; set; } = [];

    public uint[] MeshRecordBufferIds { get; set; } = [];
    public uint[] DepthMeshRecordBufferIds { get; set; } = [];

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var matrices in SkinningMatrices) matrices.Dispose();
        ColorBatches.Dispose();
        DepthBatches.Dispose();
        _meshes.Dispose();
        Snapshot.Release();
    }

    /// <summary>
    ///     Points a skinned surface at its vertices in the skinning output.
    /// </summary>
    internal void RepointVertexBuffer(int meshIndex, in DeviceBufferView vertices)
    {
        _meshes[meshIndex].VertexBuffer = vertices;
    }

    private static PooledMemory<Matrix4x4>[] ComputeSkinningMatrices(DefaultWorldSnapshot snapshot)
    {
        var skinnedGeometry = snapshot.SkinnedGeometry;
        var matrices = new PooledMemory<Matrix4x4>[skinnedGeometry.Length];
        for (var i = 0; i < skinnedGeometry.Length; i++)
        {
            ref readonly var mesh = ref skinnedGeometry[i];
            using var globalPose = new PooledMemory<Matrix4x4>(mesh.BoneCount);
            mesh.Skeleton.ResolvePose(snapshot.BonesOf(mesh), globalPose.AsSpan());
            matrices[i] = new PooledMemory<Matrix4x4>(mesh.BoneCount);
            mesh.Skeleton.ComputeSkinningMatrices(globalPose.AsSpan(), matrices[i].AsSpan());
        }

        return matrices;
    }

    public void DeclareDrawResources(IGraphConfig config, MeshBatches batches,
        Func<IMeshMaterial, IMaterialPass> selectPass)
    {
        HashSet<uint> read = [];
        HashSet<IMeshMaterial> declaredMaterials = [];
        var meshes = ProcessedMeshes;
        for (var batch = 0; batch < batches.Count; batch++)
            foreach (var index in batches[batch])
            {
                ref readonly var mesh = ref meshes[index];
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
        var staticGeometry = Snapshot.StaticGeometry;
        var skinnedGeometry = Snapshot.SkinnedGeometry;
        _meshes = new PooledMemory<ProcessedMesh>(CountSurfaces(staticGeometry, skinnedGeometry));
        var meshes = _meshes.AsSpan();
        var count = 0;

        for (var i = 0; i < staticGeometry.Length; i++)
        {
            ref readonly var mesh = ref staticGeometry[i];
            if (!TryUseMesh(builder, mesh.Mesh, out var staticIds)) continue;
            foreach (var surfaceIndex in mesh.SurfaceIndices)
                meshes[count] = new ProcessedMesh
                {
                    AbsoluteMeshIndex = count++,
                    GeometryId = i,
                    SurfaceIndex = surfaceIndex,
                    Surface = mesh.Mesh.GetSurface(surfaceIndex),
                    Transform = mesh.Transform,
                    IndexBuffer = mesh.Mesh.GetIndices(),
                    IndexBufferId = staticIds.Indices,
                    SourceVertexBufferId = staticIds.Vertices,
                    VertexBuffer = mesh.Mesh.GetVertices(surfaceIndex),
                    Material = mesh.Materials[surfaceIndex]
                };
        }

        SkinnedMeshStartIndex = count;
        ulong skinnedOutputOffset = 0;
        for (var i = 0; i < skinnedGeometry.Length; i++)
        {
            ref readonly var mesh = ref skinnedGeometry[i];
            if (!TryUseMesh(builder, mesh.Mesh, out var skinnedIds)) continue;

            foreach (var surfaceIndex in mesh.SurfaceIndices)
            {
                var surface = mesh.Mesh.GetSurface(surfaceIndex);
                var size = mesh.Mesh.GetVertexCount(surfaceIndex) * Utils.ByteSizeOf<Vertex>();
                meshes[count] = new ProcessedMesh
                {
                    AbsoluteMeshIndex = count++,
                    GeometryId = i,
                    SurfaceIndex = surfaceIndex,
                    Surface = surface,
                    Transform = mesh.Transform,
                    IndexBuffer = mesh.Mesh.GetIndices(),
                    IndexBufferId = skinnedIds.Indices,
                    SourceVertexBufferId = skinnedIds.Vertices,
                    VertexBuffer = new DeviceBufferView(ResourceHandle.InvalidBuffer, skinnedOutputOffset, size),
                    Material = mesh.Materials[surfaceIndex]
                };
                skinnedOutputOffset += size;
                SkinnedSurfaceCount++;
                SkinnedVertexCount += surface.VertexCount;
            }
        }

        // Surfaces whose mesh buffers were unavailable are skipped, so fewer may be used than were rented.
        _meshes.Truncate(count);
        ColorBatches = MeshBatches.Group(ProcessedMeshes, material => material.ColorPass);
        DepthBatches = MeshBatches.Group(ProcessedMeshes, material => material.DepthPass);
    }

    private static int CountSurfaces(ReadOnlySpan<StaticMeshInfo> staticGeometry,
        ReadOnlySpan<SkinnedMeshInfo> skinnedGeometry)
    {
        var count = 0;
        foreach (ref readonly var mesh in staticGeometry) count += mesh.SurfaceIndices.Length;
        foreach (ref readonly var mesh in skinnedGeometry) count += mesh.SurfaceIndices.Length;
        return count;
    }

    public readonly record struct BatchKey(DeviceBufferView IndexBuffer, MaterialIdentity Material);
}
