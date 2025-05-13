using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     Transitions the swapchain into present mode
/// </summary>
public class PrepareForPresentPass : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => true;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(config.SwapchainImageId, ImageLayout.PresentSrc);
    }

    public void Execute(ICompiledGraph graph, in VkCommandBuffer cmd, Frame frame, IRenderContext context)
    {
    }
}