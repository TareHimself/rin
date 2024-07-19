using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Descriptors;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Scene.Components;
using aerox.Runtime.Scene.Entities;
using TerraFX.Interop.Vulkan;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Scene.Graphics;

public class SceneDrawer : Disposable, IDrawable, ILifeCycle
{
    protected MaterialInstance? DefaultMeshMaterial;
    public DeviceBuffer? GlobalBuffer { get; private set; }
    protected MaterialInstance? DeferredRenderingMaterial;

    public GBuffer? Images { get; private set; }

    protected DeviceImage? DepthImage { get; private set; }

    public DeviceImage? RenderTarget { get; private set; }

    public readonly Scene OwningScene;

    public Vector2<uint> Size = 0;

    public SceneDrawer(Scene scene)
    {
        OwningScene = scene;
    }

    public MaterialInstance GetDefaultMeshMaterial()
    {
        if (DefaultMeshMaterial != null) return DefaultMeshMaterial;
        DefaultMeshMaterial = new MaterialInstance(Path.Join(SSceneModule.ShadersDir, "mesh_simple.ash"));
        return DefaultMeshMaterial;
    }

    protected virtual void BeginRendering(SceneFrame frame)
    {
        if (Images == null || DepthImage == null) return;
        var cmd = frame.Original.GetCommandBuffer();

        
        var attachments = Images.MakeAttachments();

        cmd.BeginRendering(new VkExtent2D()
            {
                width = (uint)Size.X,
                height = (uint)Size.Y
            }, attachments,
            SGraphicsModule.MakeRenderingAttachment(DepthImage.View,
                VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL, new VkClearValue()
                {
                    color = SGraphicsModule.MakeClearColorValue(1.0f)
                }));
        frame.Original.ConfigureForScene(Size.Cast<uint>());
    }

    protected virtual void EndRendering(SceneFrame frame)
    {
        frame.Original.GetCommandBuffer().EndRendering();
    }

    protected virtual void RunDeferredShader(Frame frame)
    {
        if (Images == null || DepthImage == null || RenderTarget == null || GlobalBuffer == null ||
            DeferredRenderingMaterial == null) return;

        var cmd = frame.GetCommandBuffer();

        RenderTarget.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            new ImageBarrierOptions()
            {
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
            });
        
        cmd.BeginRendering(new VkExtent2D()
            {
                width = (uint)Size.X,
                height = (uint)Size.Y
            },[
                SGraphicsModule.MakeRenderingAttachment(RenderTarget.View,
                    VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,new VkClearValue()
                    {
                        color = SGraphicsModule.MakeClearColorValue(0.0f)
                    })]);

            cmd.SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL)
            .DisableStencilTest(false)
            .DisableCulling()
            .DisableDepthTest()
            .EnableBlendingAlphaBlend(0, 1)
            .SetViewports([
                new VkViewport()
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

        
        // layout(set = 1, binding = 0) uniform sampler2D TColor;
        // layout(set = 1, binding = 1) uniform sampler2D TLocation;
        // layout(set = 1, binding = 2) uniform sampler2D TNormal;
        // layout(set = 1, binding = 3) uniform sampler2D TRoughMetallicSpecular;
        // layout(set = 1, binding = 5) uniform sampler2D TEmissive;
        
        
        DeferredRenderingMaterial.BindTo(frame);

        vkCmdDraw(cmd, 6, 1, 0, 0);

        cmd.EndRendering();

        RenderTarget.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
            new ImageBarrierOptions()
            {
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT,
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_FRAGMENT_SHADER_BIT
            });
    }

    protected virtual MaterialInstance CreateDeferredRenderingMaterial()
    {
        var builder = new MaterialBuilder().AddAttachmentFormats(ImageFormat.Rgba32SFloat)
            .AddShaderModules(SGraphicsModule.Get().LoadShader(Path.Join(SSceneModule.ShadersDir, "deferred.ash")));
        
        builder.Pipeline.SetInputTopology(VkPrimitiveTopology.VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST)
            .SetPolygonMode(VkPolygonMode.VK_POLYGON_MODE_FILL);

        builder.Pipeline
            .SetCullMode(VkCullModeFlags.VK_CULL_MODE_NONE, VkFrontFace.VK_FRONT_FACE_CLOCKWISE)
            .DisableMultisampling()
            .DisableBlending()
            .DisableDepthTest();

        return builder.Build();
    }

    protected virtual void SkipDraw(Frame frame)
    {
        if (RenderTarget == null) return;
        var cmd = frame.GetCommandBuffer();
        RenderTarget.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
    }

    public void Draw(Frame frame)
    {
        if (Images == null || DepthImage == null || RenderTarget == null || GlobalBuffer == null ||
            DefaultMeshMaterial == null)
        {
            SkipDraw(frame);
            return;
        }

        var targetCamera = OwningScene.ViewTarget?.FindComponent<CameraComponent>();
        if (targetCamera == null)
        {
            SkipDraw(frame);
            return;
        }

        var sceneFrame = new SceneFrame(this, frame);

        OnCollect?.Invoke(sceneFrame, Matrix4.Identity);

        if (sceneFrame.DrawCommands.Count == 0)
        {
            SkipDraw(frame);
            return;
        }


        var sceneInfo = new SceneGlobalBuffer()
        {
            CameraLocation = new Vector4<float>(targetCamera.GetWorldTransform().Location, 1.0f),
            ViewMatrix = targetCamera.GetViewMatrix(),
            ProjectionMatrix = targetCamera.GetProjection(Size.X, Size.Y),
        };

        foreach (var light in OwningScene.Lights)
        {
            sceneInfo.Lights[sceneInfo.LightsCount] = light.ToSceneLight();
            sceneInfo.LightsCount += 1;
        }


        GlobalBuffer.Write(sceneInfo);

        var cmd = frame.GetCommandBuffer();

        Images.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            new ImageBarrierOptions()
            {
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_FRAGMENT_SHADER_BIT | VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
            });
        DepthImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL, new ImageBarrierOptions()
            {
                                    SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT),
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_LATE_FRAGMENT_TESTS_BIT | VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_EARLY_FRAGMENT_TESTS_BIT
            });

        BeginRendering(sceneFrame);

        foreach (var drawCmd in sceneFrame.DrawCommands)
        {
            drawCmd.Run(sceneFrame);
        }

        EndRendering(sceneFrame);

        Images.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
            new ImageBarrierOptions()
            {
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT,
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_FRAGMENT_SHADER_BIT
            });
        
        DepthImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL,
            VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, new ImageBarrierOptions()
            {
                SubresourceRange =
                    SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT),
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_LATE_FRAGMENT_TESTS_BIT,
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_FRAGMENT_SHADER_BIT
            });
        RunDeferredShader(frame);
    }

    public void Tick(double deltaSeconds)
    {
    }

    public event Action<SceneFrame, Matrix4>? OnCollect;

    public event Action<SceneDrawer>? OnResize;
    public virtual void Resize(Vector2<uint> size)
    {
        Size = size;
        if (Images != null) SGraphicsModule.Get().WaitDeviceIdle();
        Images?.Dispose();
        DepthImage?.Dispose();
        RenderTarget?.Dispose();
        if (Size is not ({ X : 0 } and { Y : 0}))
        {
            Images = CreateGBuffer(Size);
            DepthImage = CreateBufferImage(ImageFormat.D32SFloat, Size);
            RenderTarget = CreateRenderTargetImage(ImageFormat.Rgba32SFloat, Size);
            
            DeferredRenderingMaterial?.BindImage("TColor", Images.Color, DescriptorSet.ImageType.Texture, new SamplerSpec()
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.Repeat
            });
            DeferredRenderingMaterial?.BindImage("TLocation", Images.Location, DescriptorSet.ImageType.Texture, new SamplerSpec()
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.Repeat
            });
            DeferredRenderingMaterial?.BindImage("TNormal", Images.Normal, DescriptorSet.ImageType.Texture, new SamplerSpec()
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.Repeat
            });
            DeferredRenderingMaterial?.BindImage("TRoughMetallicSpecular", Images.RoughnessMetallicSpecular, DescriptorSet.ImageType.Texture, new SamplerSpec()
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.Repeat
            });
            DeferredRenderingMaterial?.BindImage("TEmissive", Images.Emissive, DescriptorSet.ImageType.Texture, new SamplerSpec()
            {
                Filter = ImageFilter.Linear,
                Tiling = ImageTiling.Repeat
            });
            
            OnResize?.Invoke(this);
        }
    }


    protected override void OnDispose(bool isManual)
    {
        DeferredRenderingMaterial?.Dispose();
        Images?.Dispose();
        DepthImage?.Dispose();
        RenderTarget?.Dispose();
        GlobalBuffer?.Dispose();
        DefaultMeshMaterial?.Dispose();
    }

    public virtual DeviceImage CreateRenderTargetImage(ImageFormat format, Vector2<uint> size, string? debugName = null)
    {
        var imageExtent = new VkExtent3D()
        {
            width = size.X,
            height = size.Y,
            depth = 1,
        };

        var usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
                    VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT |
                    VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;

        var createInfo = SGraphicsModule.MakeImageCreateInfo(format, imageExtent, usage);
        var image = SGraphicsModule.Get().GetAllocator().NewDeviceImage(createInfo, debugName ?? "Scene GBuffer");

        var viewCreateInfo = SGraphicsModule.MakeImageViewCreateInfo(image,
            VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);

        image.View = SGraphicsModule.Get().CreateImageView(viewCreateInfo);

        return image;
    }

    public virtual DeviceImage CreateBufferImage(ImageFormat format, Vector2<uint> size, string? debugName = null)
    {
        var imageExtent = new VkExtent3D()
        {
            width = size.X,
            height = size.Y,
            depth = 1,
        };

        VkImageUsageFlags usage;

        if (format == ImageFormat.D32SFloat)
        {
            usage = VkImageUsageFlags.VK_IMAGE_USAGE_DEPTH_STENCIL_ATTACHMENT_BIT |
                    VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT;
        }
        else
        {
            usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
                    VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT |
                    VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;
        }

        var createInfo = SGraphicsModule.MakeImageCreateInfo(format, imageExtent, usage);
        var image = SGraphicsModule.Get().GetAllocator().NewDeviceImage(createInfo, debugName ?? "Scene GBuffer");

        var viewCreateInfo = SGraphicsModule.MakeImageViewCreateInfo(image,
            format == ImageFormat.D32SFloat
                ? VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT
                : VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);

        image.View = SGraphicsModule.Get().CreateImageView(viewCreateInfo);

        return image;
    }

    public GBuffer CreateGBuffer(Vector2<uint> size)
    {
        return new GBuffer(
            CreateBufferImage(ImageFormat.Rgba16UNorm, size),
            CreateBufferImage(ImageFormat.Rgba32SFloat, size), 
            CreateBufferImage(ImageFormat.Rgba32SFloat, size),
            CreateBufferImage(ImageFormat.Rgba16UNorm, size), 
            CreateBufferImage(ImageFormat.Rgba16UNorm, size)
            );
    }

    public void Start()
    {
        GlobalBuffer = SGraphicsModule.Get().GetAllocator()
            .NewUniformBuffer<SceneGlobalBuffer>(false, "Scene Global Buffer");
        DeferredRenderingMaterial = CreateDeferredRenderingMaterial();
        DeferredRenderingMaterial.BindBuffer("scene", GlobalBuffer);
        GetDefaultMeshMaterial();
    }
}