using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics.Passes;

public class StencilClearPass : IPass
{
    private readonly SurfacePassContext _surfaceContext;

    public StencilClearPass(SurfacePassContext surfaceContext)
    {
        _surfaceContext = surfaceContext;
    }

    private uint StencilImageId => _surfaceContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(StencilImageId, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var image = graph.GetImageOrException(StencilImageId);
        ctx.ClearStencilImages(0, ImageLayout.General, image);
    }
}