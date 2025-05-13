using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Views.Graphics;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Examples.ViewsTest;

public class CustomShaderPass(PassCreateInfo info) : IViewsPass
{
    private readonly IGraphicsShader
        _prettyShader =
            SGraphicsModule.Get()
                .MakeGraphics($"fs/{Path.Join(SEngine.AssetsDirectory, "test", "pretty.slang").Replace('\\', '/')}");

    private CustomShaderCommand[] _customCommands = [];
    public uint MainImageId => info.Context.MainImageId;
    public uint CopyImageId => info.Context.CopyImageId;
    public uint StencilImageId => info.Context.StencilImageId;
    private uint BufferId { get; set; }
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
    }

    public void PostAdd(IGraphBuilder builder)
    {
    }

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(MainImageId, ImageLayout.ColorAttachment);
        config.ReadImage(StencilImageId, ImageLayout.StencilAttachment);
        _customCommands = info.Commands.Cast<CustomShaderCommand>().ToArray();
        BufferId = config.CreateBuffer<Data>(_customCommands.Length, BufferStage.Graphics);
    }

    public void Execute(ICompiledGraph graph, in VkCommandBuffer cmd, Frame frame, IRenderContext context)
    {
        if (_prettyShader.Bind(cmd))
        {
            var drawImage = graph.GetImage(MainImageId);
            var stencilImage = graph.GetImage(StencilImageId);
            var view = graph.GetBufferOrException(BufferId);

            cmd.BeginRendering(info.Context.Extent.ToVk(), [
                    drawImage.MakeColorAttachmentInfo()
                ],
                stencilAttachment: stencilImage.MakeStencilAttachmentInfo()
            );

            cmd.SetViewState(info.Context.Extent);
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
            foreach (var customShaderCommand in _customCommands)
            {
                vkCmdSetStencilCompareMask(cmd, faceFlags, customShaderCommand.StencilMask);
                var pushResource = _prettyShader.PushConstants.First().Value;
                var extent = info.Context.Extent;
                var screenSize = new Vector2(extent.Width, extent.Height);
                var data = new Data
                {
                    Projection = info.Context.ProjectionMatrix,
                    ScreenSize = screenSize,
                    Transform = customShaderCommand.Transform,
                    Size = customShaderCommand.Size,
                    Time = SEngine.Get().GetTimeSeconds(),
                    Center = customShaderCommand.Hovered ? customShaderCommand.CursorPosition : screenSize / 2.0f
                };
                view.Write(data);
                cmd.PushConstant(_prettyShader.GetPipelineLayout(), pushResource.Stages, view.GetAddress());
                cmd.Draw(6);
            }

            cmd.EndRendering();
        }
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new CustomShaderPass(info);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Data
    {
        public required Matrix4x4 Projection;
        public required Vector2 ScreenSize;
        public required Matrix4x4 Transform;
        public required Vector2 Size;
        public required float Time;
        public required Vector2 Center;
    }
}