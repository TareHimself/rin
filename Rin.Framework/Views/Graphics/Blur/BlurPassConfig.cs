using JetBrains.Annotations;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Framework.Views.Graphics.Blur;

internal class BlurInitPassConfig : IPassConfig
{
    private SurfaceContext _context = null!;
    public void Init(SurfaceContext surfaceContext)
    {
        _context = surfaceContext;
    }

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(_context.MainImageId, ImageLayout.TransferSrc);
    }

    public void Begin(ICompiledGraph graph, IExecutionContext ctx)
    {
        
    }

    public void End(ICompiledGraph graph, IExecutionContext ctx)
    {
        
    }
}
public class BlurPassConfig : IPassConfig
{

    public void Init(SurfaceContext surfaceContext)
    {
    }

    public void Configure(IGraphConfig config)
    {
    }

    public void Begin(ICompiledGraph graph, IExecutionContext ctx)
    {
    }

    public void End(ICompiledGraph graph, IExecutionContext ctx)
    {
    }
}