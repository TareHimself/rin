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
    [PublicAPI] public Transform ViewTransform;
    [PublicAPI] public LightInfo[] Lights;
    [PublicAPI] public SkinnedMeshInfo[] SkinnedGeometry;
    [PublicAPI] public StaticMeshInfo[] StaticGeometry;
    [PublicAPI] public ProcessedMesh[] ProcessedGeometry = [];
    
    [PublicAPI] public Matrix4x4 View { get; set; }
    [PublicAPI] public Matrix4x4 Projection { get; }
    
    [PublicAPI] public Matrix4x4 ViewProjection { get; }
    
    [PublicAPI] public float FieldOfView { get; set; }
    [PublicAPI] public float NearClip { get; set; }
    [PublicAPI] public float FarClip { get; set; }
    
    [PublicAPI]
    public Extent2D Extent { get; }
    
    private int _firstSkinnedIndex = -1;
    
    public IEnumerable<ProcessedMesh> ProcessedStaticMeshes => ProcessedGeometry.Take(_firstSkinnedIndex >= 0 ? _firstSkinnedIndex : ProcessedGeometry.Length);
    
    public IEnumerable<ProcessedMesh> ProcessedSkinnedMeshes => ProcessedGeometry.Take(_firstSkinnedIndex >= 0 ? new Range(_firstSkinnedIndex,ProcessedGeometry.Length) : new Range(0,0));

    public WorldContext(CameraComponent viewer,in Extent2D extent)
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

    /// <summary>
    /// Called on the Render Thread
    /// </summary>
    public void Init()
    {
        var processedMeshes = StaticGeometry.SelectMany(c =>
        {
            return c.SurfaceIndices.Select(idx =>
            {
                var surface = c.Mesh.GetSurface(idx);
                return new ProcessedMesh
                {
                    Transform = c.Transform,
                    IndexBuffer = c.Mesh.GetIndices(),
                    VertexBuffer = c.Mesh.GetVertices(idx),
                    Material = c.Materials[idx],
                    IndicesCount = surface.IndicesCount,
                    IndicesStart = surface.IndicesStart,
                    VertexCount = surface.VertexCount,
                    VertexStart = surface.VertexStart,
                    Bounds = surface.Bounds,
                };
            });
        }).ToList();
        
        _firstSkinnedIndex = processedMeshes.Count;
        processedMeshes.AddRange(SkinnedGeometry.SelectMany(c =>
        {
            return c.SurfaceIndices.Select(idx =>
            {
                // Need to spoof a regular vertex buffer here since this is a skinned mesh
                var vertexBuffer = c.Mesh.GetVertices(idx);
                var offset = (vertexBuffer.Offset / Utils.ByteSizeOf<SkinnedVertex>()) * Utils.ByteSizeOf<Vertex>();
                var size = c.Mesh.GetVertexCount(idx) * Utils.ByteSizeOf<Vertex>();
                var surface = c.Mesh.GetSurface(idx);
                return new ProcessedMesh
                {
                    Transform = c.Transform,
                    IndexBuffer = c.Mesh.GetIndices(),
                    VertexBuffer = new SkinnedVertexBufferView(offset,size),
                    Material = c.Materials[idx],
                    IndicesCount = surface.IndicesCount,
                    IndicesStart = surface.IndicesStart,
                    VertexCount = surface.VertexCount,
                    VertexStart = surface.VertexStart,
                    Bounds = c.Mesh.GetBounds() // We use the full mesh bounds for 
                };
            });
        }));
            
        //_skinnedMeshes = SkinnedGeometry.Select(c => c.Mesh).Distinct().ToArray();
    }
}