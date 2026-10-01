using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Core.Views.Graphics;

public class CopyToDestination(SurfaceContext context) : IPass
{
    private uint _destinationImageId;


    public void Configure(IGraphConfig config)
    {
        config.ReadTexture(context.MainImageId, ImageLayout.TransferSrc);
        _destinationImageId = config.WriteTexture(config.DestinationImageId, ImageLayout.TransferDst);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var mainImage = graph.GetImage(context.MainImageId);
        var swapchainImage = graph.GetImage(_destinationImageId);
        ctx.CopyToImage(mainImage, swapchainImage);
    }

    public uint Id { get; set; }
}