using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics.Passes;

public class ReadBackPass(SurfacePassContext surfaceContext) : IViewsPass
{
    private uint MainImageId => surfaceContext.MainImageId;
    private uint CopyImageId => surfaceContext.CopyImageId;
    private uint StencilImageId => surfaceContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal { get; } = false;

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(MainImageId, ImageLayout.TransferSrc);
        config.WriteImage(CopyImageId, ImageLayout.TransferDst);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var mainImage = graph.GetImageOrException(MainImageId);
        var copyImage = graph.GetImageOrException(CopyImageId);
        ctx.CopyToImage(mainImage, copyImage);
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new ReadBackPass(info.Context);
    }
}