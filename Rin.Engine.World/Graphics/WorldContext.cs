using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Math;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;
using Rin.Engine.World.Mesh.Skinning;

namespace Rin.Engine.World.Graphics;

public class WorldContext
{
    [PublicAPI] public LightInfo[] Lights;
    [PublicAPI] public ProcessedMesh[] ProcessedSkinnedMeshes = [];
    [PublicAPI] public ProcessedMesh[] ProcessedStaticMeshes = [];

    [PublicAPI] public SkinnedMeshInfo[] SkinnedGeometry;
    [PublicAPI] public StaticMeshInfo[] StaticGeometry;
    [PublicAPI] public Transform ViewTransform;
    
    
    [PublicAPI] public uint[] IndirectCommandBuffers;
    [PublicAPI] public uint[] DepthIndirectCommandBuffers;
    [PublicAPI] public uint[] IndirectCommandCountBuffers;
    [PublicAPI] public uint[] DepthIndirectCommandCountBuffers;
    
    [PublicAPI] public uint[] IndirectMaterialDataBuffers;
    [PublicAPI] public uint[] DepthIndirectMaterialDataBuffers;
    public WorldContext(CameraComponent viewer, in Extent2D extent)
    {
        var world = viewer.Owner?.World ?? throw new Exception("Camera is not in a scene");
        ViewTransform = viewer.GetTransform(Space.World) with { Scale = Vector3.One };
        View = ViewTransform.ToMatrix().Inverse();
        FieldOfView = viewer.FieldOfView;
        NearClip = viewer.NearClipPlane;
        FarClip = viewer.FarClipPlane;
        Extent = extent;
        Projection = MathR.PerspectiveProjection(FieldOfView, extent,
            NearClip, FarClip);
        ViewProjection = View * Projection;
        var drawCommands = new CommandList();
        foreach (var root in world.GetPureRoots()) root.Collect(drawCommands, world.WorldTransform);
        StaticGeometry = drawCommands.StaticMeshes.ToArray();
        SkinnedGeometry = drawCommands.SkinnedMeshes.ToArray();
        Lights = drawCommands.Lights.ToArray();
    }
    
    public uint InitPassId { get; set; }

    public uint DepthImageId { get; set; }

    public uint GBufferImage0 { get; set; }
    public uint GBufferImage1 { get; set; }
    public uint GBufferImage2 { get; set; }

    public uint SkinningOutputBufferId { get; set; }

    [PublicAPI]
    public IEnumerable<ProcessedMesh> ProcessedMeshes
    {
        get
        {
            foreach (var processedStaticMesh in ProcessedStaticMeshes) yield return processedStaticMesh;

            foreach (var processedSkinnedMesh in ProcessedSkinnedMeshes) yield return processedSkinnedMesh;
        }
    }
    
    [PublicAPI] public ProcessedMesh[][] DepthIndirectGroups = [];
    [PublicAPI] public ProcessedMesh[][] IndirectGroups = [];

    [PublicAPI] public Matrix4x4 View { get; set; }
    [PublicAPI] public Matrix4x4 Projection { get; }

    [PublicAPI] public Matrix4x4 ViewProjection { get; }

    [PublicAPI] public float FieldOfView { get; set; }
    [PublicAPI] public float NearClip { get; set; }
    [PublicAPI] public float FarClip { get; set; }

    [PublicAPI] public Extent2D Extent { get; }

    private bool Culled(in Bounds3D bounds)
    {
        var viewSpaceBounds = bounds.Transform(View);


        return false;
    }

    /// <summary>
    ///     Process and cull meshes
    /// </summary>
    public void Init(uint passId)
    {
        InitPassId = passId;
        var staticMeshes = new List<ProcessedMesh>();
        var skeletalMeshes = new List<ProcessedMesh>();
        foreach (var mesh in StaticGeometry)
        foreach (var surfaceIndex in mesh.SurfaceIndices)
        {
            var surface = mesh.Mesh.GetSurface(surfaceIndex);
            var bounds = surface.Bounds.Transform(mesh.Transform);
            if (!Culled(bounds))
                staticMeshes.Add(new ProcessedMesh
                {
                    Transform = mesh.Transform,
                    IndexBuffer = mesh.Mesh.GetIndices(),
                    VertexBuffer = mesh.Mesh.GetVertices(surfaceIndex),
                    Material = mesh.Materials[surfaceIndex],
                    IndicesCount = surface.IndicesCount,
                    IndicesStart = surface.IndicesStart,
                    VertexCount = surface.VertexCount,
                    VertexStart = surface.VertexStart,
                    Bounds = bounds
                });
        }

        foreach (var mesh in SkinnedGeometry)
        {
            var bounds = mesh.Mesh.GetBounds().Transform(mesh.Transform);
            foreach (var surfaceIndex in mesh.SurfaceIndices)
            {
                var surface = mesh.Mesh.GetSurface(surfaceIndex);
                var vertexBuffer = mesh.Mesh.GetVertices(surfaceIndex);
                var offset = vertexBuffer.Offset / Utils.ByteSizeOf<SkinnedVertex>() * Utils.ByteSizeOf<Vertex>();
                var size = mesh.Mesh.GetVertexCount(surfaceIndex) * Utils.ByteSizeOf<Vertex>();

                if (!Culled(bounds))
                    skeletalMeshes.Add(new ProcessedMesh
                    {
                        Transform = mesh.Transform,
                        IndexBuffer = mesh.Mesh.GetIndices(),
                        VertexBuffer = new SkinnedVertexBufferView(offset, size),
                        Material = mesh.Materials[surfaceIndex],
                        IndicesCount = surface.IndicesCount,
                        IndicesStart = surface.IndicesStart,
                        VertexCount = surface.VertexCount,
                        VertexStart = surface.VertexStart,
                        Bounds = bounds // We use the full mesh bounds for skinned meshes 
                    });
            }
        }


        ProcessedStaticMeshes = staticMeshes.ToArray();
        ProcessedSkinnedMeshes = skeletalMeshes.ToArray();
        
        IndirectGroups = ProcessedMeshes.GroupBy((c) => c, new ProcessedMesh.CompareIndirectBatch()).Select(c => c.ToArray()).ToArray();
        DepthIndirectGroups = ProcessedMeshes.GroupBy((c) => c, new ProcessedMesh.CompareIndirectBatchDepth()).Select(c => c.ToArray()).ToArray();
    }
}