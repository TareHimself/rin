using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Framework.Views.Graphics;

public class CopySurfaceToSwapchain(SurfaceContext context) : IPass
{
    private uint _swapchainImageId;

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(context.MainImageId, ImageLayout.TransferSrc);
        _swapchainImageId = config.WriteImage(config.SwapchainImageId, ImageLayout.TransferDst);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var mainImage = graph.GetImageOrException(context.MainImageId);
        var swapchainImage = graph.GetImageOrException(_swapchainImageId);
        ctx.CopyToImage(mainImage, swapchainImage);
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
}