﻿using System.Runtime.InteropServices;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Descriptors;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Shaders.Rsl;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Views.Graphics.Commands;

[StructLayout(LayoutKind.Sequential)]
public struct BlurPushConstants
{
    public required Mat4 Projection;
    
    public required Mat3 Transform;

    public required Vec2<float> Size;

    public required float BlurRadius;

    public required Vec4<float> Tint;
}

public class BlurCommand(Mat3 transform, Vec2<float> size,float radius, Vec4<float> tint) : CustomCommand
{
    private static string _blurPassId = Guid.NewGuid().ToString();
    private readonly IGraphicsShader _blurRslShader = SGraphicsModule.Get().GetShaderManager().GraphicsFromPath(Path.Join(SViewsModule.ShadersDirectory,"blur.slang"));

    public override bool WillDraw => true;


    // public static void ApplyBlurPass(WidgetFrame frame)
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
    //     frame.Raw.ConfigureForWidgets(size.Cast<uint>());
    // }

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBuffer? buffer = null)
    {
        frame.BeginMainPass();
        var cmd = frame.Raw.GetCommandBuffer();
        if (_blurRslShader.Bind(cmd,true))
        {
            var copyImage = frame.DrawImage;
            var descriptorSet = frame.Raw.GetDescriptorAllocator()
                .Allocate(_blurRslShader.GetDescriptorSetLayouts().First().Value);
            descriptorSet.WriteImages(_blurRslShader.Resources["SourceT"].Binding, new ImageWrite(copyImage,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL, ImageType.Texture, new SamplerSpec()
                {
                    Filter = ImageFilter.Linear,
                    Tiling = ImageTiling.ClampBorder
                }));

            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _blurRslShader.GetPipelineLayout(),
                new []{descriptorSet});

            var pushResource = _blurRslShader.PushConstants.First().Value;
            var push = new BlurPushConstants()
            {
                Projection = frame.Projection,
                Size = size,
                BlurRadius = radius,
                Tint = tint,
                Transform = transform
            };
            cmd.PushConstant(_blurRslShader.GetPipelineLayout(), pushResource.Stages, push);
            vkCmdDraw(cmd,6,1,0,0);
        }
    }
}