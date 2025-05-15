using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.World.Graphics;

public class FillGBufferPass : IPass
{
    private WorldContext _worldContext;
    public FillGBufferPass(WorldContext worldContext)
    {
        _worldContext = worldContext;
    }

    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;
    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(_worldContext.DepthImageId, ImageLayout.DepthAttachment);
        _worldContext.GBufferImage0 = config.CreateImage(_worldContext.Extent, ImageFormat.RGBA32, ImageLayout.ColorAttachment);
        _worldContext.GBufferImage1 = config.CreateImage(_worldContext.Extent,ImageFormat.RGBA32, ImageLayout.ColorAttachment);
        _worldContext.GBufferImage2 = config.CreateImage(_worldContext.Extent,ImageFormat.RGBA32, ImageLayout.ColorAttachment);
    }

    public void Execute(ICompiledGraph graph,IExecutionContext ctx)
    {
        var cmd = ctx.GetCommandBuffer();
        //var gbuffer0 = graph.
    }
}