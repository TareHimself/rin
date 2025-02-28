﻿using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Views.Graphics.Commands;

public struct BlurPushConstants
{
    public required Mat4 Projection;

    public required Mat3 Transform;

    public required Vector2 Size;

    public required float Strength;

    public required float Radius;

    public required Vector4 Tint;
}

public class BlurCommand(Mat3 transform, Vector2 size, float strength, float radius, Vector4 tint) : CustomCommand
{
    private static string _blurPassId = Guid.NewGuid().ToString();

    private readonly IGraphicsShader _blurShader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SViewsModule.ShadersDirectory, "blur.slang"));

    public override bool WillDraw()
    {
        return true;
    }

    public override ulong GetRequiredMemory()
    {
        return 0;
    }
    // public static void ApplyBlurPass(ViewFrame frame)
    // {
    //     var cmd = frame.Raw.GetCommandBuffer();
    //
    //     var drawImage = frame.Surface.GetDrawImage();
    //     var stencilImage = frame.Surface.GetStencilImage();
    //
    //     var size = frame.Surface.GetDrawSize();
    //
    //     var drawExtent = new VkExtent3D
    //     {
    //         width = (uint)size.X,
    //         height = (uint)size.Y
    //     };
    //
    //     cmd.BeginRendering(new VkExtent2D
    //     {
    //         width = drawExtent.width,
    //         height = drawExtent.height
    //     }, [
    //         SGraphicsModule.MakeRenderingAttachment(drawImage.View,
    //             VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL)
    //     ], stencilAttachment: SGraphicsModule.MakeRenderingAttachment(stencilImage.View,
    //         VkImageLayout.VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL));
    //
    //     frame.Raw.ConfigureForViews(size.Cast<uint>());
    // }

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        frame.BeginMainPass();
        var cmd = frame.Raw.GetCommandBuffer();
        if (_blurShader.Bind(cmd, true))
        {
            var resource = _blurShader.Resources["SourceT"];
            var copyImage = frame.CopyImage;
            var descriptorSet = frame.Raw.GetDescriptorAllocator()
                .Allocate(_blurShader.GetDescriptorSetLayouts()[resource.Set]);
            descriptorSet.WriteImages(resource.Binding, new ImageWrite(copyImage,
                ImageLayout.ShaderReadOnly, ImageType.Sampled, new SamplerSpec
                {
                    Filter = ImageFilter.Linear,
                    Tiling = ImageTiling.ClampBorder
                }));

            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _blurShader.GetPipelineLayout(),
                new[] { descriptorSet });

            var pushResource = _blurShader.PushConstants.First().Value;
            var push = new BlurPushConstants
            {
                Projection = frame.Projection,
                Size = size,
                Strength = strength,
                Radius = radius,
                Tint = tint,
                Transform = transform
            };
            cmd.PushConstant(_blurShader.GetPipelineLayout(), pushResource.Stages, push);
            cmd.Draw(6);
        }
    }
}