using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Core.Views.Graphics.PassConfigs;

public class MainPassConfig : IPassConfig
{
    private SurfaceContext _context = null!;
    [PublicAPI] public uint MainImageId => _context.MainImageId;

    [PublicAPI] public uint StencilImageId => _context.StencilImageId;

    public void Init(SurfaceContext surfaceContext)
    {
        _context = surfaceContext;
    }

    public void Configure(IGraphConfig config)
    {
        config.WriteTexture(MainImageId, ImageLayout.ColorAttachment);
        config.ReadTexture(StencilImageId, ImageLayout.StencilAttachment);
    }

    public void Begin(ICompiledGraph graph, IExecutionContext ctx)
    {
        var drawImage = graph.GetImage(MainImageId);
        var stencilImage = graph.GetImage(StencilImageId);

        ctx.BeginRendering(_context.Extent, [drawImage], stencilAttachment: stencilImage)
            .DisableFaceCulling()
            .StencilCompareOnly();
    }

    public void End(ICompiledGraph graph, IExecutionContext ctx)
    {
        ctx.EndRendering();
    }
}