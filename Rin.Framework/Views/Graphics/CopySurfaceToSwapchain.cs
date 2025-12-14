using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;

namespace Rin.Framework.Views.Graphics;

public class CopySurfaceToSwapchain(SurfaceContext context) : IPass
{
    private uint _swapchainImageId;

    public Action? OnPrune => null;

    public void Configure(IGraphConfig config)
    {
        config.ReadTexture(context.MainImageId, ImageLayout.TransferSrc);
        _swapchainImageId = config.WriteTexture(config.SwapchainImageId, ImageLayout.TransferDst);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var mainImage = graph.GetTexture(context.MainImageId);
        var swapchainImage = graph.GetTexture(_swapchainImageId);
        ctx.CopyToImage(mainImage, swapchainImage);
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
}