using System.Numerics;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.World.Components;
using Rin.World.Graphics.Mesh;
using Rin.World.Math;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics.Default;

public class DefaultWorldRenderContext : IWorldRenderContext
{
    /// <summary>
    ///     All skinned geometry in this world , filled in the constructor
    /// </summary>
    [PublicAPI] public readonly SkinnedMeshInfo[] SkinnedGeometry;

    /// <summary>
    ///     All non skinned geometry in this world , filled in the constructor
    /// </summary>
    [PublicAPI] public readonly StaticMeshInfo[] StaticGeometry;

    private ProcessedMesh[] _processedMeshes;
    [PublicAPI] public uint[] DepthIndirectCommandBuffers;
    [PublicAPI] public uint[] DepthIndirectCommandCountBuffers;

    [PublicAPI] public ProcessedMesh[][] DepthIndirectGroups = [];
    [PublicAPI] public uint[] DepthIndirectMaterialDataBuffers;

    [PublicAPI] public uint[] IndirectCommandBuffers;
    [PublicAPI] public uint[] IndirectCommandCountBuffers;
    [PublicAPI] public ProcessedMesh[][] IndirectGroups = [];

    [PublicAPI] public uint[] IndirectMaterialDataBuffers;

    [PublicAPI] public int TotalMeshCount;

    [PublicAPI] public Frustum ViewFrustum;


    [PublicAPI] public Transform ViewTransform;

    /// <summary>
    ///     Takes proxy data already resolved by <see cref="DefaultRenderSystem" /> — no tree walk here.
    /// </summary>
    public DefaultWorldRenderContext(CameraComponent viewer, in Extent2D extent, StaticMeshInfo[] staticGeometry,
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

    public bool WillDoSkinning => SkinnedGeometry.NotEmpty();

    [PublicAPI] public uint BoundsBufferId { get; set; }

    [PublicAPI] public uint CullingOutputBufferId { get; set; }

    public uint InitPassId { get; set; }

    public uint SkinningPassId { get; set; }
    public uint DepthImageId { get; set; }

    public uint GBufferImage0 { get; set; }
    public uint GBufferImage1 { get; set; }
    public uint GBufferImage2 { get; set; }
    public uint GBufferImage3 { get; set; }

    public uint SkinningOutputBufferId { get; set; }

    public LightInfo[] Lights { get; }
    public ProcessedMesh[] ProcessedStaticMeshes { get; private set; } = [];
    public ProcessedMesh[] ProcessedSkinnedMeshes { get; private set; } = [];
    public ProcessedMesh[] ProcessedMeshes { get; private set; } = [];

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

    /// <summary>
    ///     Process and cull meshes
    /// </summary>
    public void Initialize(uint passId)
    {
        InitPassId = passId;
        var staticMeshes = new List<ProcessedMesh>();
        var skeletalMeshes = new List<ProcessedMesh>();

        foreach (var mesh in StaticGeometry)
        foreach (var surfaceIndex in mesh.SurfaceIndices)
        {
            var surface = mesh.Mesh.GetSurface(surfaceIndex);
            staticMeshes.Add(new ProcessedMesh
            {
                Id = TotalMeshCount++,
                Transform = mesh.Transform,
                IndexBuffer = mesh.Mesh.GetIndices(),
                VertexBuffer = mesh.Mesh.GetVertices(surfaceIndex),
                Material = mesh.Materials[surfaceIndex],
                IndicesCount = surface.IndicesCount,
                IndicesStart = surface.IndicesStart,
                VertexCount = surface.VertexCount,
                VertexStart = surface.VertexStart,
                Bounds = surface.Bounds
            });
        }


        foreach (var mesh in SkinnedGeometry)
        foreach (var surfaceIndex in mesh.SurfaceIndices)
        {
            var surface = mesh.Mesh.GetSurface(surfaceIndex);
            var vertexBuffer = mesh.Mesh.GetVertices(surfaceIndex);
            var offset = vertexBuffer.Offset / Utils.ByteSizeOf<SkinnedVertex>() * Utils.ByteSizeOf<Vertex>();
            var size = mesh.Mesh.GetVertexCount(surfaceIndex) * Utils.ByteSizeOf<Vertex>();

            skeletalMeshes.Add(new ProcessedMesh
            {
                Id = TotalMeshCount++,
                Transform = mesh.Transform,
                IndexBuffer = mesh.Mesh.GetIndices(),
                VertexBuffer = new DeviceBufferView(ResourceHandle.InvalidBuffer, offset, size),
                Material = mesh.Materials[surfaceIndex],
                IndicesCount = surface.IndicesCount,
                IndicesStart = surface.IndicesStart,
                VertexCount = surface.VertexCount,
                VertexStart = surface.VertexStart,
                Bounds = surface.Bounds // This is recomputed in the shaders
            });
        }

        ProcessedStaticMeshes = staticMeshes.ToArray();
        ProcessedSkinnedMeshes = skeletalMeshes.ToArray();

        staticMeshes.AddRange(skeletalMeshes);

        ProcessedMeshes = staticMeshes.ToArray();

        IndirectGroups = ProcessedMeshes.GroupBy(c => c, new ProcessedMesh.CompareIndirectBatch())
            .Select(c => c.ToArray()).ToArray();
        DepthIndirectGroups = ProcessedMeshes.GroupBy(c => c, new ProcessedMesh.CompareIndirectBatchDepth())
            .Select(c => c.ToArray()).ToArray();
    }
}