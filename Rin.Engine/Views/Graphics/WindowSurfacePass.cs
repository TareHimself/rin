using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics;

public class WindowSurfacePass(Surface surface, Vector2 drawSize,SharedPassContext passContext) : IPass
{
    public void Added(IGraphBuilder builder)
    {
        // _viewsPassId = builder.AddPass(_viewsPass);
    }

    public void Configure(IGraphConfig config)
    {
        config.Read(passContext.MainImageId);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var mainImage = graph.GetImageOrException(passContext.MainImageId);
        var cmd = frame.GetCommandBuffer();
        cmd.ImageBarrier(mainImage, ImageLayout.TransferSrc);
        frame.OnCopy += (_, image) => { cmd.CopyImageToImage(mainImage, image); };
    }

    public uint Id { get; set; }
    public bool IsTerminal => true;

    public void Dispose()
    {
    }
}