using System.Numerics;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Shared;
using Rin.Core.Shared.Math;
using Rin.World.Components;
using Rin.World.Graphics.Default.Passes;
using Rin.World.Graphics.Mesh;
using Rin.World.Math;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics.Default;

public class DefaultWorldCollectedData : IWorldCollectedData, IDisposable
{
    /// <summary>
    ///     All skinned geometry in this world , filled in the constructor
    /// </summary>
    [PublicAPI] public readonly SkinnedMeshInfo[] SkinnedGeometry;

    /// <summary>
    ///     All non skinned geometry in this world , filled in the constructor
    /// </summary>
    [PublicAPI] public readonly StaticMeshInfo[] StaticGeometry;

    [PublicAPI] public uint[] DepthIndirectCommandBuffers;
    [PublicAPI] public uint[] DepthIndirectCommandCountBuffers;

    [PublicAPI] public IReadOnlyDictionary<BatchKey,List<ProcessedMesh>> DepthIndirectGroups { get; private set; }

    [PublicAPI] public uint[] IndirectCommandBuffers;
    [PublicAPI] public uint[] IndirectCommandCountBuffers;
    [PublicAPI] public IReadOnlyDictionary<BatchKey,List<ProcessedMesh>> IndirectGroups { get; private set; }


    [PublicAPI] public int TotalMeshCount;

    [PublicAPI] public Frustum ViewFrustum;


    [PublicAPI] public Transform ViewTransform;

    /// <summary>
    ///     Takes proxy data already resolved by <see cref="DefaultRenderSystem" /> — no tree walk here.
    /// </summary>
    public DefaultWorldCollectedData(CameraComponent viewer, in Extent2D extent, StaticMeshInfo[] staticGeometry,
        SkinnedMeshInfo[] skinnedGeometry, LightInfo[] lights)
    {
        ViewTransform = viewer.GetTransform(Space.World) with { Scale = Vector3.One };
        View = ViewTransform.ToMatrix().Inverse();
        FieldOfView = viewer.FieldOfView;
        NearClip = viewer.NearClipPlane;
        FarClip = viewer.FarClipPlane;
        Extent = extent;
        Projection = MathR.PerspectiveProjection(FieldOfView, extent,
            NearClip, FarClip);
        ViewProjection = View * Projection;
        StaticGeometry = staticGeometry;
        SkinnedGeometry = skinnedGeometry;
        Lights = lights;
        ViewFrustum = MathR.ExtractWorldSpaceFrustum(View, Projection, ViewProjection);
    }

    [PublicAPI] public uint BoundsBufferId { get; set; }

    public uint DepthImageId { get; set; }

    public uint GBufferImage0 { get; set; }
    public uint GBufferImage1 { get; set; }
    public uint GBufferImage2 { get; set; }
    public uint GBufferImage3 { get; set; }

    public uint SkinningOutputBufferId { get; set; }

    public LightInfo[] Lights { get; }
    public int SkinnedMeshStartIndex { get; private set; } = 0;
    public List<PooledMemory<Matrix4x4>> SkinnedPoses { get; private set; } = [];
    public IReadOnlyList<ProcessedMesh> ProcessedMeshes { get; private set; } = [];
    public int ProcessedSkinnedMeshCount => ProcessedMeshes.Count - SkinnedMeshStartIndex;
    public int SkinnedSurfaceCount { get; set; } = 0;
    public long SkinnedVertexCount { get; set; } = 0;
    public int StaticSurfaceCount { get; set; } = 0;
    public long StaticVertexCount { get; set; } = 0;

    public uint OutputImageId { get; set; }

    [PublicAPI] public Matrix4x4 View { get; set; }
    [PublicAPI] public Matrix4x4 Projection { get; }

    [PublicAPI] public Matrix4x4 ViewProjection { get; }

    [PublicAPI] public float FieldOfView { get; set; }
    [PublicAPI] public float NearClip { get; set; }
    [PublicAPI] public float FarClip { get; set; }

    [PublicAPI] public Extent2D Extent { get; }

    public uint GetOutputImageId()
    {
        return OutputImageId;
    }

    public uint GetGBufferImageId(int index)
    {
        return index switch
        {
            0 => GBufferImage0,
            1 => GBufferImage1,
            2 => GBufferImage2,
            3 => GBufferImage3,
            _ => 0
        };
    }

    public LightInfo[] GetLights()
    {
        return Lights;
    }
    
    
    private readonly Dictionary<IMesh, (uint Vertices, uint Indices)> _meshBufferIds = [];

    private bool TryUseMesh(IGraphBuilder builder, IMesh mesh, out (uint Vertices, uint Indices) ids)
    {
        if (_meshBufferIds.TryGetValue(mesh, out ids)) return true;

        ids = (builder.AddExternalBuffer(mesh.GetVertices()), builder.AddExternalBuffer(mesh.GetIndices()));
        if (ids.Vertices == 0 || ids.Indices == 0) return false;

        _meshBufferIds.Add(mesh, ids);
        return true;
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

    public void Write(IGraphBuilder builder)
    {
        
        var meshes = new List<ProcessedMesh>();

        for (var i = 0; i < StaticGeometry.Length; i++)
        {
            var mesh = StaticGeometry[i];
            if (!TryUseMesh(builder, mesh.Mesh, out var staticIds)) continue;
            foreach (var surfaceIndex in mesh.SurfaceIndices)
            {
                var surface = mesh.Mesh.GetSurface(surfaceIndex);
                meshes.Add(new ProcessedMesh
                {
                    AbsoluteMeshIndex = TotalMeshCount++,
                    GeometryId = i,
                    SurfaceIndex = surfaceIndex,
                    Surface = surface,
                    Transform = mesh.Transform,
                    IndexBuffer = mesh.Mesh.GetIndices(),
                    IndexBufferId = staticIds.Indices,
                    SourceVertexBufferId = staticIds.Vertices,
                    VertexBuffer = mesh.Mesh.GetVertices(surfaceIndex),
                    Material = mesh.Materials[surfaceIndex],
                });
                
                StaticSurfaceCount++;
                StaticVertexCount += surface.VertexCount;
            }
        }
        
        SkinnedMeshStartIndex = meshes.Count;
        builder.AddDisposable(this);
        SkinnedPoses.EnsureCapacity(SkinnedGeometry.Length);
        ulong skinnedOutputOffset = 0;
        for (var i = 0; i < SkinnedGeometry.Length; i++)
        {
            var mesh = SkinnedGeometry[i];
            
            // whole bind matrix shebang
            using var globalPose = mesh.Skeleton.ResolvePosePooled(mesh.Pose);
            var skinnedPose = new PooledMemory<Matrix4x4>(globalPose.Count);
            mesh.Skeleton.ComputeSkinningMatrices(globalPose.AsSpan(), skinnedPose.AsSpan());
            SkinnedPoses.Add(skinnedPose);
            if (!TryUseMesh(builder, mesh.Mesh, out var skinnedIds)) continue;

            foreach (var surfaceIndex in mesh.SurfaceIndices)
            {
                var surface = mesh.Mesh.GetSurface(surfaceIndex);
                var size = mesh.Mesh.GetVertexCount(surfaceIndex) * Utils.ByteSizeOf<Vertex>();
                var offset = skinnedOutputOffset;
                skinnedOutputOffset += size;

                meshes.Add(new ProcessedMesh
                {
                    AbsoluteMeshIndex = TotalMeshCount++,
                    GeometryId = i,
                    SurfaceIndex = surfaceIndex,
                    Surface = surface, // bounds are static here
                    Transform = mesh.Transform,
                    IndexBuffer = mesh.Mesh.GetIndices(),
                    IndexBufferId = skinnedIds.Indices,
                    SourceVertexBufferId = skinnedIds.Vertices,
                    VertexBuffer = new DeviceBufferView(ResourceHandle.InvalidBuffer, offset, size),
                    Material = mesh.Materials[surfaceIndex]
                });
                SkinnedSurfaceCount++;
                SkinnedVertexCount +=  surface.VertexCount;
            }
        }
        

        ProcessedMeshes = meshes;
        var colorGroups = new Dictionary<BatchKey,List<ProcessedMesh>>();
        var depthGroups = new Dictionary<BatchKey,List<ProcessedMesh>>();

        for (var i = 0; i < meshes.Count; i++)
        {
            var mesh = meshes[i];
            var colorGroupKey = new BatchKey(mesh.IndexBuffer, mesh.Material.GetColorIdentity());
            var depthGroupKey = new BatchKey(mesh.IndexBuffer, mesh.Material.GetDepthIdentity());
            if (!colorGroups.TryGetValue(colorGroupKey, out var colorGroup))
            {
                colorGroup = colorGroups[colorGroupKey] = [];
            }
            if (!depthGroups.TryGetValue(depthGroupKey, out var depthGroup))
            {
                depthGroup = depthGroups[depthGroupKey] = [];
            }

            colorGroup.Add(mesh);
            depthGroup.Add(mesh);
        }
        
        IndirectGroups = colorGroups;
        DepthIndirectGroups = depthGroups;
        
        builder.AddPass(new InitWorldResourcesPass(this));

        if (ProcessedSkinnedMeshCount > 0)
        {
            builder.AddPass(new SkinningPass(this));
            builder.AddPass(new BoundsUpdatePass(this));
        }

        {
            var cullingPass = new CullingPass(this);
            builder.AddPass(cullingPass);
            builder.AddPass(new FillIndirectBuffersPass(cullingPass, this));
        }

        builder.AddPass(new DepthPrepassIndirectPass(this));
        builder.AddPass(new FillGBufferIndirectPass(this));

        builder.AddPass(new LightingPass(this));
    }

    public void Dispose()
    {
        foreach (var pose in SkinnedPoses) pose.Dispose();
        SkinnedPoses.Clear();
    }

    public readonly record struct BatchKey(DeviceBufferView IndexBuffer, MaterialIdentity Material);
}