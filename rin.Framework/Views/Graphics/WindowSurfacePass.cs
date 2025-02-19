using System.Numerics;
using rin.Framework.Graphics;
using rin.Framework.Graphics.FrameGraph;

namespace rin.Framework.Views.Graphics;

public class WindowSurfacePass(Surface surface, Vector2 drawSize, PassInfo passInfo) : IPass
{
    private uint _viewsImageId;
    private readonly ViewsPass _viewsPass = new(surface, drawSize, passInfo);
    private uint _viewsPassId;

    public void Dispose()
    {
    }

    public void BeforeAdd(IGraphBuilder builder)
    {
        _viewsPassId = builder.AddPass(_viewsPass);
    }

    public void Configure(IGraphConfig config)
    {
        config.DependOn(_viewsPassId);
        _viewsImageId = config.Read(_viewsPass.MainImageId);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var mainImage = graph.GetImage(_viewsImageId);
        var cmd = frame.GetCommandBuffer();
        cmd.ImageBarrier(mainImage, ImageLayout.TransferSrc);
        frame.OnCopy += (_, image) => { cmd.CopyImageToImage(mainImage, image); };
    }

    public uint Id { get; set; }
    public bool IsTerminal => true;
}