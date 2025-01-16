using JetBrains.Annotations;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Scene.Components;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Scene.Graphics;

/// <summary>
/// Collects the scene, and builds the GBuffer
/// </summary>
/// <param name="camera">The perspective the scene is collected from</param>
/// <param name="size"></param>
public class CollectScenePass(CameraComponent camera, Vec2<uint> size) : IPass
{
    
    /// <summary>
    /// Color(RGB), Roughness(A) Image Id
    /// </summary>
    [PublicAPI]
    public uint ColorRoughnessId { get; private set; }
    /// <summary>
    /// Location(RGB), Metallic(A) Image Id
    /// </summary>
    [PublicAPI]
    public uint LocationMetallicId { get; private set; }
    /// <summary>
    /// Normal(RGB), Specular(A) Image ID
    /// </summary>
    [PublicAPI]
    public uint NormalSpecularId { get; private set; }
    
    /// <summary>
    /// Depth Image ID
    /// </summary>
    [PublicAPI]
    public uint DepthImageId { get; private set; }
    
    [PublicAPI]
    public IDeviceImage? ColorRoughnessImage { get; set; }
    [PublicAPI]
    public IDeviceImage? LocationMetallicImage { get; set; }
    [PublicAPI]
    public IDeviceImage? NormalSpecularImage { get; set; }
    [PublicAPI]
    public IDeviceImage? DepthImage { get; set; }

    [PublicAPI] public Mat4 View { get; set; } = camera.GetSceneTransform().Mutate(c =>
    {
        c.Scale = new Vec3<float>(1.0f);
        return ((Mat4)c).Inverse();
    });
    [PublicAPI]
    public Mat4 Projection { get; } = Glm.Perspective(camera.FieldOfView, (float)size.X / (float)size.Y,camera.NearClipPlane,camera.FarClipPlane);
    [PublicAPI]
    public float FieldOfView { get; set; } = camera.FieldOfView;
    [PublicAPI]
    public float NearClip { get; set; } = camera.NearClipPlane;
    [PublicAPI]
    public float FarClip { get; set; } = camera.FarClipPlane;
    [PublicAPI]
    public Vec2<uint> Size { get; set; } = size;
    [PublicAPI]
    public GeometryInfo[] Geometry = [];
    [PublicAPI]
    public LightInfo[] Lights = [];
    [PublicAPI]
    public Scene Scene  = camera.Owner?.Scene ?? throw new Exception("Camera is not in a scene");
    
    private uint BufferId { get; set; }

    public void Configure(IGraphConfig config)
    {
        ColorRoughnessId = config.CreateImage(Size.X, Size.Y,ImageFormat.RGBA32);
        LocationMetallicId = config.CreateImage(Size.X, Size.Y,ImageFormat.RGBA32);
        NormalSpecularId = config.CreateImage(Size.X, Size.Y,ImageFormat.RGBA32);
        DepthImageId = config.CreateImage(Size.X, Size.Y,ImageFormat.Depth);
        var drawCommands = new rin.Framework.Scene.Graphics.DrawCommands();

        foreach (var root in Scene.GetPureRoots().ToArray())
        {
            root.Collect(drawCommands,Mat4.Identity);
        }

        Geometry = drawCommands.GeometryCommands.ToArray();
        Lights = drawCommands.Lights.ToArray();

        var bufferSize = Geometry.Aggregate<GeometryInfo, ulong>(0, (current, geometryDrawCommand) => current + geometryDrawCommand.Material.GetRequiredMemory());
        
        BufferId = bufferSize > 0 ? config.AllocateBuffer(bufferSize) : 0;
    }

    public void Execute(ICompiledGraph graph, Frame frame, VkCommandBuffer cmd)
    {
        ColorRoughnessImage = graph.GetResource(ColorRoughnessId).AsImage();
        LocationMetallicImage = graph.GetResource(LocationMetallicId).AsImage();
        NormalSpecularImage = graph.GetResource(NormalSpecularId).AsImage();
        DepthImage = graph.GetResource(DepthImageId).AsImage();
        IDeviceImage[] colorImages = [ColorRoughnessImage,LocationMetallicImage,NormalSpecularImage];
        foreach (var deviceImage in colorImages)
        {
            cmd.ImageBarrier(deviceImage, ImageLayout.Undefined, ImageLayout.General);
        }
        cmd.ImageBarrier(DepthImage, ImageLayout.Undefined, ImageLayout.General, new ImageBarrierOptions()
        {
            SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
        });

        cmd.ClearColorImages(0.0f, ImageLayout.General, colorImages);
        cmd.ClearDepthImages(1.0f, ImageLayout.General, DepthImage);
        
        var colorAttachments = colorImages.Select(c =>
        {
            cmd.ImageBarrier(c, ImageLayout.General, ImageLayout.ColorAttachment);
            return c.MakeColorAttachmentInfo(0.0f);
        }).ToArray();
        
        cmd.ImageBarrier(DepthImage, ImageLayout.General, ImageLayout.DepthAttachment, new ImageBarrierOptions()
        {
            SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT)
        });
        
        var depthAttachment = DepthImage.MakeDepthAttachmentInfo();
        
        cmd.BeginRendering(Size.ToVkExtent(),colorAttachments,depthAttachment);
        cmd
            .SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE,VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .EnableDepthTest(true,VkCompareOp.VK_COMPARE_OP_LESS_OR_EQUAL)
            .DisableBlending(0, 5)
            .SetVertexInput([],[])
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
        var sceneFrame = new SceneFrame(frame,View,Projection);
        var buffer = BufferId > 0 ? graph.GetResource(BufferId).AsMemory() : null;
        ulong offset = 0;
        foreach (var geometryInfos in Geometry.GroupBy(c => new
                 {
                     Type = c.Material.GetType(),
                     c.Surface.StartIndex,
                     c.Surface.Count,
                     c.Geometry.IndexBuffer,
                 }))
        {
            var infos = geometryInfos.ToArray();
            var size = infos.Aggregate<GeometryInfo, ulong>(0, (current, geometryDrawCommand) => current + geometryDrawCommand.Material.GetRequiredMemory());
            using var view = buffer?.GetView(offset, size);
            offset += size;
            infos.First().Material.Execute(sceneFrame,view,infos);
        }

        cmd.EndRendering();
        
        foreach (var deviceImage in colorImages)
        {
            cmd.ImageBarrier(deviceImage, ImageLayout.ColorAttachment, ImageLayout.ShaderReadOnly);
        }
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
    
    public void Dispose()
    {
    }
}