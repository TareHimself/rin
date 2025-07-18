﻿using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics;

public class CopySurfaceToSwapchain(SurfacePassContext passContext) : IPass
{
    private uint _swapchainImageId;

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(passContext.MainImageId, ImageLayout.TransferSrc);
        _swapchainImageId = config.WriteImage(config.SwapchainImageId, ImageLayout.TransferDst);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var mainImage = graph.GetImageOrException(passContext.MainImageId);
        var swapchainImage = graph.GetImageOrException(_swapchainImageId);
        ctx.CopyToImage(swapchainImage, mainImage);
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
}