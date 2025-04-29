using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics;

public class WindowSurfacePass(Surface surface, Vector2 drawSize,SharedPassContext passContext) : IPass
{
    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        // _viewsPassId = builder.AddPass(_viewsPass);
    }

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(passContext.MainImageId,ImageLayout.ShaderReadOnly);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var mainImage = graph.GetImageOrException(passContext.MainImageId);
        var cmd = frame.GetCommandBuffer();
        frame.OnCopy += (_, image) => { cmd.CopyImageToImage(mainImage, image); };
    }

    public uint Id { get; set; }
    public bool IsTerminal => true;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;

    public void Dispose()
    {
    }
}