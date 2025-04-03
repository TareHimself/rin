using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Math;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;
using TerraFX.Interop.Vulkan;
using Utils = Rin.Engine.Utils;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Collects the scene, and does a depth pre-pass
/// </summary>
public class CollectScenePass : IPass
{
    [PublicAPI] public GeometryInfo[] Geometry = [];
    [PublicAPI] public LightInfo[] Lights = [];
    [PublicAPI] public GeometryInfo[] OpaqueGeometry = [];
    [PublicAPI] public GeometryInfo[] TranslucentGeometry = [];
    [PublicAPI] public World World;

    /// <summary>
    ///     Collects the scene, and does a depth pre-pass
    /// </summary>
    /// <param name="camera">The perspective the scene is collected from</param>
    /// <param name="size"></param>
    public CollectScenePass(CameraComponent camera, Vector2<uint> size)
    {
        World = camera.Owner?.World ?? throw new Exception("Camera is not in a scene");
        View = camera.GetTransform(Space.World).Mutate(c =>
        {
            c.Scale = new Vector3(1.0f);
            return (camera.Owner.World.WorldTransform * c.ToMatrix()).Inverse();
        });
        View = Matrix4x4.Identity; // View needs work, assume we are at the center for now
        Projection = MathR.PerspectiveProjection(90.0f, size.X, size.Y,
            0.0001f, 2000);
        ViewProjection = Projection * View;
        FieldOfView = camera.FieldOfView;
        NearClip = camera.NearClipPlane;
        FarClip = camera.FarClipPlane;
        Size = size;
        
        var test = new Vector3(50,0,1000);
        var result = test.Project(Projection);
        
        var test2 = new Vector3(-50,10,50f);
        var result2 = test2.Project(Projection);
        var x = Matrix4x4.Identity;
        
        // Collect Scene On Main Thread
        var drawCommands = new DrawCommands();
        var world = Matrix4x4.CreateWorld(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
        foreach (var root in World.GetPureRoots().ToArray()) root.Collect(drawCommands, world);
        Geometry = drawCommands.GeometryCommands.ToArray();
        OpaqueGeometry = Geometry.Where(c => !c.MeshMaterial.Translucent).ToArray();
        TranslucentGeometry = Geometry.Where(c => c.MeshMaterial.Translucent).ToArray();
        Lights = drawCommands.Lights.ToArray();
    }

    /// <summary>
    ///     Depth Image ID
    /// </summary>
    [PublicAPI]
    public uint DepthImageId { get; private set; }

    [PublicAPI] public IGraphImage? DepthImage { get; set; }

    [PublicAPI]
    public Matrix4x4 View { get; set; }

    [PublicAPI]
    public Matrix4x4 Projection { get; }
    
    public Matrix4x4 ViewProjection { get; }
    [PublicAPI] public float FieldOfView { get; set; }
    [PublicAPI] public float NearClip { get; set; }
    [PublicAPI] public float FarClip { get; set; }
    [PublicAPI] public Vector2<uint> Size { get; set; }
    
    private uint DepthSceneBufferId { get; set; }
    private uint DepthMaterialBufferId { get; set; }

    
    public void Added(IGraphBuilder builder)
    {
        
    }
    

    public void Configure(IGraphConfig config)
    {
        DepthImageId = config.CreateImage(Size.X, Size.Y, ImageFormat.Depth);
        DepthSceneBufferId = config.AllocateBuffer<DepthSceneInfo>();
        var depthMaterialDataSize = OpaqueGeometry.Aggregate(Utils.ByteSizeOf<DepthSceneInfo>(),
            (current, geometryDrawCommand) => current + geometryDrawCommand.MeshMaterial.DepthPass.GetRequiredMemory());

        DepthMaterialBufferId = depthMaterialDataSize > 0 ? config.AllocateBuffer(depthMaterialDataSize) : 0;
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
            .ClearDepthImages(1.0f, ImageLayout.General, DepthImage)
            .ImageBarrier(DepthImage, ImageLayout.DepthAttachment)
            .BeginRendering(Size.ToVkExtent(), [], DepthImage.MakeDepthAttachmentInfo())
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL)
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

        foreach (var geometryInfos in OpaqueGeometry.GroupBy(c => new
                 {
                     Type = c.MeshMaterial.GetType(),
                     c.Mesh
                 }))
        {
            var infos = geometryInfos.ToArray();
            var first = infos.First();
            var size = first.MeshMaterial.DepthPass.GetRequiredMemory() * (ulong)infos.Length;
            var view = materialDataBuffer?.GetView(materialDataBufferOffset, size);
            materialDataBufferOffset += size;
            first.MeshMaterial.DepthPass.Execute(sceneFrame, view, infos);
        }

        cmd.EndRendering();
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = false;

    public void Dispose()
    {
    }
}