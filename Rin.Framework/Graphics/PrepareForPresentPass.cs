using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Framework.Graphics;

/// <summary>
///     Transitions the swapchain into present mode
/// </summary>
public class PrepareForPresentPass : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => true;

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(config.SwapchainImageId, ImageLayout.PresentSrc);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
    }
}