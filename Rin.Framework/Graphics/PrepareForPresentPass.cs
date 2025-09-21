using Rin.Framework.Graphics.Graph;

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
        config.WriteTexture(config.SwapchainImageId, ImageLayout.PresentSrc);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
    }
}