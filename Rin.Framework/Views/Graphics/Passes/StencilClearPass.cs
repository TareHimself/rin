using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Vulkan.Graph;

namespace Rin.Framework.Views.Graphics.Passes;

public class StencilClearPass : IPass
{
    private readonly SurfaceContext _surfaceContext;

    public StencilClearPass(SurfaceContext surfaceContext)
    {
        _surfaceContext = surfaceContext;
    }

    private uint StencilImageId => _surfaceContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        config.WriteTexture(StencilImageId, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var image = graph.GetTextureOrException(StencilImageId);
        ctx.ClearStencilImages(0,  image);
    }
}