using JetBrains.Annotations;
using Rin.Framework.Views.Graphics.Commands;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Vulkan.Graph;

namespace Rin.Framework.Views.Graphics.PassConfigs;

public class MainPassConfig : IPassConfig
{
    [PublicAPI] public uint MainImageId => _context.MainImageId;

    [PublicAPI] public uint StencilImageId => _context.StencilImageId;

    private SurfaceContext _context = null!;

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
        var drawImage = graph.GetTexture(MainImageId);
        var stencilImage = graph.GetTexture(StencilImageId);

        ctx.BeginRendering(_context.Extent, [drawImage], stencilAttachment: stencilImage)
            .DisableFaceCulling()
            .StencilCompareOnly();
    }

    public void End(ICompiledGraph graph, IExecutionContext ctx)
    {
        ctx.EndRendering();
    }
}