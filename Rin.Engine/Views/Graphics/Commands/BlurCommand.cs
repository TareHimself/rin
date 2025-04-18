﻿using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Views.Graphics.Commands;

internal struct BlurData()
{
    public required Matrix4x4 Projection = Matrix4x4.Identity;

    public required Matrix4x4 Transform = Matrix4x4.Identity;

    private Vector4 _options = Vector4.Zero;

    public Vector2 Size
    {
        get => new(_options.X, _options.Y);
        set
        {
            _options.X = value.X;
            _options.Y = value.Y;
        }
    }

    public float Strength
    {
        get => _options.Z;
        set => _options.Z = value;
    }

    public float Radius
    {
        get => _options.W;
        set => _options.W = value;
    }

    public Vector4 Tint = Vector4.Zero;
}

public class BlurCommand(Matrix4x4 transform, Vector2 size, float strength, float radius, Vector4 tint) : CustomCommand
{
    private static string _blurPassId = Guid.NewGuid().ToString();

    private readonly IGraphicsShader _blurShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/blur.slang");

    public override bool WillDraw()
    {
        return true;
    }

    public override ulong GetRequiredMemory()
    {
        return Utils.ByteSizeOf<BlurData>();
    }

    public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBufferView? view = null)
    {
        var buffer = view ?? throw new NullReferenceException();
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
                [descriptorSet]);

            var pushResource = _blurShader.PushConstants.First().Value;
            buffer.Write(new BlurData
            {
                Projection = frame.Projection,
                Size = size,
                Strength = strength,
                Radius = radius,
                Tint = tint,
                Transform = transform
            });
            cmd.PushConstant(_blurShader.GetPipelineLayout(), pushResource.Stages, buffer.GetAddress());
            cmd.Draw(6);
        }
    }
}