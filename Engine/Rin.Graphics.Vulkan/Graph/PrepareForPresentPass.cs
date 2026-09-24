using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
///     Transitions the swapchain into present mode
/// </summary>
internal class PrepareForPresentPass : ITerminalPass
{
    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        config.WriteTexture(config.SwapchainImageId, ImageLayout.Present);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
    }
}