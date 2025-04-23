using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Descriptors;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics.Commands;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Views.Graphics.Passes.Blur;

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

public class BlurPass : IViewsPass
{
    private readonly IGraphicsShader _blurShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/blur.slang");

    private readonly SharedPassContext _sharedContext;
    private BlurCommand[] _blurCommands;

    public BlurPass(PassCreateInfo info)
    {
        _sharedContext = info.Context;
        _blurCommands = info.Commands.Cast<BlurCommand>().ToArray();
    }

    private uint MainImageId => _sharedContext.MainImageId;
    private uint CopyImageId => _sharedContext.CopyImageId;
    private uint StencilImageId => _sharedContext.StencilImageId;
    private uint _bufferId = 0;
    public uint Id { get; set; }
    public bool IsTerminal { get; } = false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        
    }

    public void Configure(IGraphConfig config)
    {
        config.Write(MainImageId);
        config.Read(CopyImageId);
        config.Write(StencilImageId);
        _bufferId = config.AllocateBuffer<BlurData>(_blurCommands.Length);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var cmd = frame.GetCommandBuffer();
        if (_blurShader.Bind(cmd, true))
        {
            var drawImage = graph.GetImage(MainImageId);
            var copyImage = graph.GetImage(CopyImageId);
            var stencilImage = graph.GetImage(StencilImageId);
            var buffer = graph.GetBufferOrException(_bufferId);
            cmd
                .ImageBarrier(drawImage, ImageLayout.ColorAttachment)
                .ImageBarrier(copyImage, ImageLayout.ShaderReadOnly)
                .ImageBarrier(stencilImage, ImageLayout.StencilAttachment);

            // foreach (var command in _passInfo.PreCommands) command.Execute(viewFrame);
            cmd.BeginRendering(_sharedContext.Extent.ToVk(), [
                    drawImage.MakeColorAttachmentInfo()
                ],
                stencilAttachment: stencilImage.MakeStencilAttachmentInfo()
            );

            frame.ConfigureForViews(_sharedContext.Extent);
            var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;

            vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);

            cmd.SetWriteMask(0, 1,
                VkColorComponentFlags.VK_COLOR_COMPONENT_R_BIT |
                VkColorComponentFlags.VK_COLOR_COMPONENT_G_BIT |
                VkColorComponentFlags.VK_COLOR_COMPONENT_B_BIT |
                VkColorComponentFlags.VK_COLOR_COMPONENT_A_BIT);

            var compareMask = uint.MaxValue;

            var resource = _blurShader.Resources["SourceT"];
            var descriptorSet = frame.GetDescriptorAllocator()
                .Allocate(_blurShader.GetDescriptorSetLayouts()[resource.Set]);
            descriptorSet.WriteImages(resource.Binding, new ImageWrite(copyImage,
                ImageLayout.ShaderReadOnly, ImageType.Sampled, new SamplerSpec
                {
                    Filter = ImageFilter.Linear,
                    Tiling = ImageTiling.ClampBorder
                }));

            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _blurShader.GetPipelineLayout(),
                [descriptorSet]);

            foreach (var blur in _blurCommands)
            {
                vkCmdSetStencilCompareMask(cmd, faceFlags, compareMask);
                var pushResource = _blurShader.PushConstants.First().Value;
                buffer.Write(new BlurData
                {
                    Projection = _sharedContext.ProjectionMatrix,
                    Size = blur.Size,
                    Strength = blur.Strength,
                    Radius = blur.Radius,
                    Tint = blur.Tint,
                    Transform = blur.Transform
                });
                cmd.PushConstant(_blurShader.GetPipelineLayout(), pushResource.Stages, buffer.GetAddress());
                cmd.Draw(6);
            }

            cmd.EndRendering();
        }
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new BlurPass(info);
    }
}