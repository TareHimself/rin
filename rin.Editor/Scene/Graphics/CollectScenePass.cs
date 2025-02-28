using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Editor.Scene.Components;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using Utils = Rin.Engine.Core.Utils;

namespace Rin.Editor.Scene.Graphics;

/// <summary>
/// Collects the scene, and does a depth pre-pass
/// </summary>
/// <param name="camera">The perspective the scene is collected from</param>
/// <param name="size"></param>
public class CollectScenePass(CameraComponent camera, Vector2<uint> size) : IPass
{
    /// <summary>
    /// Depth Image ID
    /// </summary>
    [PublicAPI]
    public uint DepthImageId { get; private set; }

    [PublicAPI] public IDeviceImage? DepthImage { get; set; }

    [PublicAPI]
    public Mat4 View { get; set; } = camera.GetSceneTransform().Mutate(c =>
    {
        c.Scale = new Vector3(1.0f);
        return ((Mat4)c).Inverse();
    });

    [PublicAPI]
    public Mat4 Projection { get; } = Glm.Perspective(camera.FieldOfView, (float)size.X / (float)size.Y,
        camera.NearClipPlane, camera.FarClipPlane);

    [PublicAPI] public float FieldOfView { get; set; } = camera.FieldOfView;
    [PublicAPI] public float NearClip { get; set; } = camera.NearClipPlane;
    [PublicAPI] public float FarClip { get; set; } = camera.FarClipPlane;
    [PublicAPI] public Vector2<uint> Size { get; set; } = size;
    [PublicAPI] public GeometryInfo[] Geometry = [];
    [PublicAPI] public GeometryInfo[] OpaqueGeometry = [];
    [PublicAPI] public GeometryInfo[] TranslucentGeometry = [];
    [PublicAPI] public LightInfo[] Lights = [];
    [PublicAPI] public Scene Scene = camera.Owner?.Scene ?? throw new Exception("Camera is not in a scene");
    
    
    private uint DepthSceneBufferId { get; set; }
    private uint DepthMaterialBufferId { get; set; }

    public void BeforeAdd(IGraphBuilder builder)
    {
    }

    public void Configure(IGraphConfig config)
    {
        DepthImageId = config.CreateImage(Size.X, Size.Y, ImageFormat.Depth);
        var drawCommands = new DrawCommands();

        foreach (var root in Scene.GetPureRoots().ToArray())
        {
            root.Collect(drawCommands, Mat4.Identity);
        }

        Geometry = drawCommands.GeometryCommands.ToArray();
        OpaqueGeometry = Geometry.Where(c => !c.MeshMaterial.Translucent).ToArray();
        TranslucentGeometry = Geometry.Where(c => c.MeshMaterial.Translucent).ToArray();
        Lights = drawCommands.Lights.ToArray();

        DepthSceneBufferId = config.AllocateBuffer<DepthSceneInfo>();
        var depthMaterialDataSize = OpaqueGeometry.Aggregate(Utils.ByteSizeOf<DepthSceneInfo>(),
            (current, geometryDrawCommand) => current + geometryDrawCommand.MeshMaterial.DepthPass.GetRequiredMemory());
        
        DepthMaterialBufferId = depthMaterialDataSize > 0 ? config.AllocateBuffer(depthMaterialDataSize) : 0;
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var sceneDataBuffer = graph.GetBuffer(DepthSceneBufferId);
        var materialDataBuffer = DepthMaterialBufferId > 0 ? graph.GetBuffer(DepthMaterialBufferId) : null;
        ulong materialDataBufferOffset = 0;

        DepthImage = graph.GetImage(DepthImageId).AsImage();

        context.ImageBarrier(DepthImage, ImageLayout.Undefined, ImageLayout.General, new ImageBarrierOptions()
            {
                SubresourceRange =
                    SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
            })
            .ClearDepthImages(1.0f, ImageLayout.General, DepthImage)
            .ImageBarrier(DepthImage, ImageLayout.General, ImageLayout.DepthAttachment, new ImageBarrierOptions()
            {
                SubresourceRange =
                    SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
            });

        var depthAttachment = DepthImage.MakeDepthAttachmentInfo();

        context.BeginRendering(Size.ToVkExtent(), [], depthAttachment);
        context
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(true, VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL)
            //.DisableBlending(0, 0)
            .SetVertexInput([], [])
            .SetViewports([
                // For viewport flipping
                new VkViewport()
                {
                    x = 0,
                    y = Size.Y,
                    width = Size.X,
                    height = -Size.Y,
                    minDepth = 0.0f,
                    maxDepth = 1.0f
                }
            ])
            .SetScissors([
                new VkRect2D()
                {
                    offset = new VkOffset2D(),
                    extent = new VkExtent2D()
                    {
                        width = Size.X,
                        height = Size.Y
                    }
                }
            ]);
        
        var sceneFrame = new SceneFrame(frame, View, Projection,sceneDataBuffer);

        sceneDataBuffer.Write(new DepthSceneInfo()
        {
            View = sceneFrame.View,
            Projection = sceneFrame.Projection,
            ViewProjection = sceneFrame.ViewProjection
        });

        foreach (var geometryInfos in OpaqueGeometry.GroupBy(c => new
                 {
                     Type = c.MeshMaterial.GetType(),
                     c.Geometry.IndexBuffer,
                 }))
        {
            var infos = geometryInfos.ToArray();
            var first = infos.First();
            var size = first.MeshMaterial.DepthPass.GetRequiredMemory() * (ulong)infos.Length;
            var view = materialDataBuffer?.GetView(materialDataBufferOffset, size);
            materialDataBufferOffset += size;
            first.MeshMaterial.DepthPass.Execute(sceneFrame, view, infos);
        }

        context.EndRendering();
    }

    public uint Id { get; set; }
    public bool IsTerminal { get; set; } = false;

    public void Dispose()
    {
    }
}