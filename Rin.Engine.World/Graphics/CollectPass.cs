using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Math;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;
using Rin.Engine.World.Mesh.Skinning;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

/// <summary>
///  Collects the <see cref="Rin.Engine.World.World"/>, performs skinning and does a depth pre-pass
/// </summary>
public class CollectPass : IPass
{
    [PublicAPI] public Transform CameraTransform;
    [PublicAPI] public StaticMeshInfo[] Geometry;
    [PublicAPI] public LightInfo[] Lights;
    [PublicAPI] public SkinnedMeshInfo[] SkinnedGeometry;
    [PublicAPI] public StaticMeshInfo[] StaticGeometry;
    [PublicAPI] public ProcessedMesh[] ProcessedGeometry;
    [PublicAPI] public World World;

    /// <summary>
    ///     Collects the scene, and does a depth pre-pass
    /// </summary>
    /// <param name="camera">The perspective the scene is collected from</param>
    /// <param name="size"></param>
    public CollectPass(CameraComponent camera, Vector2<uint> size)
    {
        World = camera.Owner?.World ?? throw new Exception("Camera is not in a scene");
        CameraTransform = camera.GetTransform(Space.World);
        View = (CameraTransform with { Scale = Vector3.One }).ToMatrix().Inverse();
        FieldOfView = camera.FieldOfView;
        NearClip = camera.NearClipPlane;
        FarClip = camera.FarClipPlane;
        Projection = MathR.PerspectiveProjection(FieldOfView, size.X, size.Y,
            NearClip, FarClip);
        ViewProjection = View * Projection;
        Size = size;
        // Collect Scene On Main Thread
        var drawCommands = new DrawCommands();
        foreach (var root in World.GetPureRoots().ToArray()) root.Collect(drawCommands, World.WorldTransform);
        Geometry = drawCommands.StaticMeshes.ToArray();
        StaticGeometry = drawCommands.StaticMeshes.ToArray();
        SkinnedGeometry = drawCommands.SkinnedMeshes.ToArray();
        Lights = drawCommands.Lights.ToArray();
    }

    /// <summary>
    ///     Depth Image ID
    /// </summary>
    [PublicAPI]
    public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }

    [PublicAPI] public Matrix4x4 View { get; set; }

    [PublicAPI] public Matrix4x4 Projection { get; }

    public Matrix4x4 ViewProjection { get; }
    [PublicAPI] public float FieldOfView { get; set; }
    [PublicAPI] public float NearClip { get; set; }
    [PublicAPI] public float FarClip { get; set; }
    [PublicAPI] public Vector2<uint> Size { get; set; }

    private uint SkinnedMeshBufferId { get; set; }
    private uint SkinningExecutionInfoBufferId { get; set; }
    private uint SkinningPoseBufferId { get; set; }
    private uint SkinningOutputBufferId { get; set; }
    private uint DepthSceneBufferId { get; set; }
    private uint DepthMaterialBufferId { get; set; }
    private int _firstSkinnedIndex;

    public void Added(IGraphBuilder builder)
    {
    }


    public void Configure(IGraphConfig config)
    {
        DepthImageId = config.CreateImage(Size.X, Size.Y, ImageFormat.Depth);
        DepthSceneBufferId = config.AllocateBuffer<DepthSceneInfo>();
        List<ProcessedMesh> processedMeshes = StaticGeometry.SelectMany(c =>
        {
            return c.SurfaceIndices.Select(idx =>
            {
                var surface = c.Mesh.GetSurface(idx);
                return new ProcessedMesh
                {
                    IndexBuffer = c.Mesh.GetIndices(),
                    VertexBuffer = c.Mesh.GetVertices(idx),
                    Material = c.Materials[idx],
                    IndicesCount = surface.IndicesCount,
                    IndicesStart = surface.IndicesStart,
                    VertexCount = surface.VertexCount,
                    VertexStart = surface.VertexStart,
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
                    IndexBuffer = c.Mesh.GetIndices(),
                    VertexBuffer = new SkinnedVertexBufferView(offset,size),
                    Material = c.Materials[idx],
                    IndicesCount = surface.IndicesCount,
                    IndicesStart = surface.IndicesStart,
                    VertexCount = surface.VertexCount,
                    VertexStart = surface.VertexStart,
                };
            });
        }));
        
        ProcessedGeometry = processedMeshes.ToArray();
        
        var depthMaterialDataSize = ProcessedGeometry.Aggregate(Utils.ByteSizeOf<DepthSceneInfo>(),
            (current, gcmd) => current + gcmd.Material.DepthPass.GetRequiredMemory());
        
        DepthMaterialBufferId = depthMaterialDataSize > 0 ? config.AllocateBuffer(depthMaterialDataSize) : 0;

    // private uint SkinnedMeshBufferId { get; set; }
    // private uint SkinningExecutionInfoBufferId { get; set; }
    // private uint SkinningPoseBufferId { get; set; }
    // private uint SkinningOutputBufferId { get; set; }
    
        var uniqueSkinnedGeometry = SkinnedGeometry.Select(c => c.Mesh).ToHashSet().ToArray();

        if (uniqueSkinnedGeometry.Length > 0)
        {
            var totalVertices = uniqueSkinnedGeometry.Aggregate<IMesh,ulong>(0,(t,c) => t + c.GetVertexCount());   
        }
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var cmd = frame.GetCommandBuffer();
        var sceneDataBuffer = graph.GetBuffer(DepthSceneBufferId);
        var materialDataBuffer = DepthMaterialBufferId > 0 ? graph.GetBuffer(DepthMaterialBufferId) : null;
        ulong materialDataBufferOffset = 0;
        DepthImage = graph.GetImage(DepthImageId);

        cmd
            .ImageBarrier(DepthImage, ImageLayout.General)
            .ClearDepthImages(0.0f, ImageLayout.General, DepthImage)
            .ImageBarrier(DepthImage, ImageLayout.DepthAttachment)
            .BeginRendering(Size.ToVkExtent(), [], DepthImage.MakeDepthAttachmentInfo())
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_GREATER_OR_EQUAL)
            //.DisableBlending(0, 0)
            .SetVertexInput([], [])
            .SetViewports([
                // For viewport flipping
                new VkViewport
                {
                    x = 0,
                    y = 0,
                    width = Size.X,
                    height = Size.Y,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D
                    {
                        width = Size.X,
                        height = Size.Y
                    }
                }
            ]);

        var sceneFrame = new SceneFrame(frame, View, Projection, sceneDataBuffer);

        sceneDataBuffer.Write(new DepthSceneInfo
        {
            View = sceneFrame.View,
            Projection = sceneFrame.Projection,
            ViewProjection = sceneFrame.ViewProjection
        });

        foreach (var geometryInfos in ProcessedGeometry.GroupBy(c => new
                 {
                     Type = c.Material.GetType(),
                     c.IndexBuffer
                 }))
        {
            var infos = geometryInfos.ToArray();
            var first = infos.First();
            var size = first.Material.DepthPass.GetRequiredMemory() * (ulong)infos.Length;
            var view = materialDataBuffer?.GetView(materialDataBufferOffset, size);
            materialDataBufferOffset += size;
            first.Material.DepthPass.Execute(sceneFrame, view, infos);
        }

        cmd.EndRendering();
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = false;

    public void Dispose()
    {
    }
}