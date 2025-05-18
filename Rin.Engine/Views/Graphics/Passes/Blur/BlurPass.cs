using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Views.Graphics.Commands;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Views.Graphics.Passes.Blur;

internal struct BlurData()
{
    public required ImageHandle SourceT;

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
    private readonly BlurCommand[] _blurCommands;

    private readonly IGraphicsShader _blurShader = SGraphicsModule.Get()
        .MakeGraphics("Engine/Shaders/Views/blur.slang");

    private readonly SharedPassContext _sharedContext;
    private uint _bufferId;

    public BlurPass(PassCreateInfo info)
    {
        _sharedContext = info.Context;
        _blurCommands = info.Commands.Cast<BlurCommand>().ToArray();
    }

    private uint MainImageId => _sharedContext.MainImageId;
    private uint CopyImageId => _sharedContext.CopyImageId;
    private uint StencilImageId => _sharedContext.StencilImageId;
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
        config.WriteImage(MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(CopyImageId, ImageLayout.ShaderReadOnly);
        config.ReadImage(StencilImageId, ImageLayout.StencilAttachment);
        _bufferId = config.CreateBuffer<BlurData>(_blurCommands.Length, BufferStage.Graphics);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var cmd = ctx.GetCommandBuffer();

        if (_blurShader.Bind(cmd))
        {
            var drawImage = graph.GetImage(MainImageId);
            var copyImage = graph.GetImage(CopyImageId);
            var stencilImage = graph.GetImage(StencilImageId);
            var buffer = graph.GetBufferOrException(_bufferId);

            Debug.Assert(copyImage.BindlessHandle != ImageHandle.InvalidImage, "copyImage bindless handle is invalid");

            cmd.BeginRendering(_sharedContext.Extent, [
                    drawImage.MakeColorAttachmentInfo()
                ],
                stencilAttachment: stencilImage.MakeStencilAttachmentInfo()
            );

            cmd.SetViewState(_sharedContext.Extent);
            var faceFlags = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK;

            vkCmdSetStencilOp(cmd, faceFlags, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
                VkCompareOp.VK_COMPARE_OP_NOT_EQUAL);

            var compareMask = uint.MaxValue;

            foreach (var blur in _blurCommands)
            {
                cmd.SetStencilCompareMask(compareMask);
                buffer.Write(new BlurData
                {
                    SourceT = copyImage.BindlessHandle,
                    Projection = _sharedContext.ProjectionMatrix,
                    Size = blur.Size,
                    Strength = blur.Strength,
                    Radius = blur.Radius,
                    Tint = blur.Tint,
                    Transform = blur.Transform
                });
                _blurShader.Push(cmd, buffer.GetAddress());
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