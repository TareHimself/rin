using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace Rin.Engine.Views.Graphics.Passes;

public class CreateImagesPass : IPass
{
    public SharedPassContext Context { get; set; }

    public CreateImagesPass(SharedPassContext context)
    {
        Context = context;
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
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
        Context.MainImageId = config.CreateImage(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.CopyImageId = config.CreateImage(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.StencilImageId = config.CreateImage(Context.Extent, ImageFormat.Stencil, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var cmd = frame.GetCommandBuffer();
        var drawImage = graph.GetImage(Context.MainImageId);
        var copyImage = graph.GetImage(Context.CopyImageId);
        var stencilImage = graph.GetImage(Context.StencilImageId);
        
        ResetStencilState(cmd);

        cmd
            .ImageBarrier(drawImage, ImageLayout.General)
            .ImageBarrier(copyImage, ImageLayout.General)
            .ImageBarrier(stencilImage, ImageLayout.General)
            .ClearColorImages(new Vector4(0.0f), ImageLayout.General, drawImage, copyImage)
            .ClearStencilImages(0, stencilImage.Layout, stencilImage);
    }
    
    private static void ResetStencilState(VkCommandBuffer cmd,
        VkStencilFaceFlags faceMask = VkStencilFaceFlags.VK_STENCIL_FACE_FRONT_AND_BACK)
    {
        vkCmdSetStencilTestEnable(cmd, 1);
        vkCmdSetStencilReference(cmd, faceMask, 255);
        vkCmdSetStencilWriteMask(cmd, faceMask, 0x01);
        vkCmdSetStencilCompareMask(cmd, faceMask, 0x01);
        vkCmdSetStencilOp(cmd, faceMask, VkStencilOp.VK_STENCIL_OP_KEEP, VkStencilOp.VK_STENCIL_OP_KEEP,
            VkStencilOp.VK_STENCIL_OP_KEEP, VkCompareOp.VK_COMPARE_OP_NEVER);
    }
    
}