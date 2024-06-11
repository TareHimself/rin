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
    public DeviceBuffer? GlobalBuffer { get; protected set; }
    protected MaterialInstance? DeferredRenderingMaterial;

    protected GBuffer? Images { get; private set; }

    protected DeviceImage? DepthImage { get; private set; }

    public DeviceImage? RenderTarget { get; private set; }

    public readonly Scene OwningScene;

    public Vector2<int> Size = 0;

    public SceneDrawer(Scene scene)
    {
        OwningScene = scene;
    }

    public MaterialInstance GetDefaultMeshMaterial()
    {
        if (DefaultMeshMaterial != null) return DefaultMeshMaterial;
        DefaultMeshMaterial = new MaterialBuilder().ConfigureForScene().SetShader(SGraphicsModule.Get()
                .LoadShader(Path.Join(SSceneModule.ShadersDir, "mesh_simple.ash")))
            .Build();
        return DefaultMeshMaterial;
    }

    protected virtual void BeginRendering(SceneFrame frame)
    {
        if (Images == null || DepthImage == null) return;
        var cmd = frame.Raw.GetCommandBuffer();


        var renderingInfo = SGraphicsModule.MakeRenderingInfo(new VkExtent2D()
        {
            width = (uint)Size.X,
            height = (uint)Size.Y
        });


        unsafe
        {
            var attachments = Images.MakeAttachments();
            fixed (VkRenderingAttachmentInfo* pAttachments = attachments)
            {
                renderingInfo.colorAttachmentCount = (uint)attachments.Length;
                renderingInfo.pColorAttachments = pAttachments;

                var depthAttachment = SGraphicsModule.MakeRenderingAttachment(DepthImage.View,
                    VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL, new VkClearValue()
                    {
                        color = SGraphicsModule.MakeClearColorValue(1.0f)
                    });

                renderingInfo.pDepthAttachment = &depthAttachment;
                

                vkCmdBeginRendering(cmd, &renderingInfo);
            }
        }
    }

    protected virtual void EndRendering(SceneFrame frame)
    {
        vkCmdEndRendering(frame.Raw.GetCommandBuffer());
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

        var renderingInfo = SGraphicsModule.MakeRenderingInfo(new VkExtent2D()
        {
            width = (uint)Size.X,
            height = (uint)Size.Y
        });

        unsafe
        {
            var attachment = SGraphicsModule.MakeRenderingAttachment(RenderTarget.View,
                VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,new VkClearValue()
                {
                    color = SGraphicsModule.MakeClearColorValue(new Vector4<float>(0.0f))
                });

            renderingInfo.colorAttachmentCount = 1;
            renderingInfo.pColorAttachments = &attachment;

            vkCmdBeginRendering(cmd, &renderingInfo);
        }

        DeferredRenderingMaterial.BindTo(frame);
        // layout(set = 1, binding = 0) uniform sampler2D TColor;
        // layout(set = 1, binding = 1) uniform sampler2D TLocation;
        // layout(set = 1, binding = 2) uniform sampler2D TNormal;
        // layout(set = 1, binding = 3) uniform sampler2D TRoughMetallicSpecular;
        // layout(set = 1, binding = 5) uniform sampler2D TEmissive;
        DeferredRenderingMaterial.BindImage("TColor", Images.Color, DescriptorSet.ImageType.Texture, new SamplerSpec()
        {
            Filter = EImageFilter.Linear,
            Tiling = EImageTiling.Repeat
        });
        DeferredRenderingMaterial.BindImage("TLocation", Images.Location, DescriptorSet.ImageType.Texture, new SamplerSpec()
        {
            Filter = EImageFilter.Linear,
            Tiling = EImageTiling.Repeat
        });
        DeferredRenderingMaterial.BindImage("TNormal", Images.Normal, DescriptorSet.ImageType.Texture, new SamplerSpec()
        {
            Filter = EImageFilter.Linear,
            Tiling = EImageTiling.Repeat
        });
        DeferredRenderingMaterial.BindImage("TRoughMetallicSpecular", Images.RoughnessMetallicSpecular, DescriptorSet.ImageType.Texture, new SamplerSpec()
        {
            Filter = EImageFilter.Linear,
            Tiling = EImageTiling.Repeat
        });
        DeferredRenderingMaterial.BindImage("TEmissive", Images.Emissive, DescriptorSet.ImageType.Texture, new SamplerSpec()
        {
            Filter = EImageFilter.Linear,
            Tiling = EImageTiling.Repeat
        });

        vkCmdDraw(cmd, 6, 1, 0, 0);

        vkCmdEndRendering(cmd);

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
        return new MaterialBuilder().AddAttachmentFormats(EImageFormat.Rgba32SFloat)
            .SetShader(SGraphicsModule.Get().LoadShader(Path.Join(SSceneModule.ShadersDir, "deferred.ash"))).Build();
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

    public virtual void OnResize(Vector2<int> size)
    {
        Size = size;
        if (Images != null) SGraphicsModule.Get().WaitDeviceIdle();
        Images?.Dispose();
        DepthImage?.Dispose();
        RenderTarget?.Dispose();
        if (Size is not { X : 0, Y : 0 })
        {
            Images = CreateGBuffer(Size);
            DepthImage = CreateBufferImage(EImageFormat.D32SFloat, Size);
            RenderTarget = CreateRenderTargetImage(EImageFormat.Rgba32SFloat, Size);
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

    public virtual DeviceImage CreateRenderTargetImage(EImageFormat format, Vector2<int> size, string? debugName = null)
    {
        var imageExtent = new VkExtent3D()
        {
            width = (uint)size.X,
            height = (uint)size.Y,
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

    public virtual DeviceImage CreateBufferImage(EImageFormat format, Vector2<int> size, string? debugName = null)
    {
        var imageExtent = new VkExtent3D()
        {
            width = (uint)size.X,
            height = (uint)size.Y,
            depth = 1,
        };

        VkImageUsageFlags usage;

        if (format == EImageFormat.D32SFloat)
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
            format == EImageFormat.D32SFloat
                ? VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT
                : VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);

        image.View = SGraphicsModule.Get().CreateImageView(viewCreateInfo);

        return image;
    }

    public GBuffer CreateGBuffer(Vector2<int> size)
    {
        return new GBuffer(
            CreateBufferImage(EImageFormat.Rgba16UNorm, size),
            CreateBufferImage(EImageFormat.Rgba32SFloat, size), 
            CreateBufferImage(EImageFormat.Rgba32SFloat, size),
            CreateBufferImage(EImageFormat.Rgba16UNorm, size), 
            CreateBufferImage(EImageFormat.Rgba16UNorm, size)
            );
    }

    public void Start()
    {
        GlobalBuffer = SGraphicsModule.Get().GetAllocator()
            .NewUniformBuffer<SceneGlobalBuffer>(false, "Scene Global Buffer");
        DeferredRenderingMaterial = CreateDeferredRenderingMaterial();
        GetDefaultMeshMaterial();
    }
}