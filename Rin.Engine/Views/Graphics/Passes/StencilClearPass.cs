using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Views.Graphics.Passes;

public class StencilClearPass : IPass
{
    private readonly SharedPassContext _sharedContext;

    public StencilClearPass(SharedPassContext sharedContext)
    {
        _sharedContext = sharedContext;
    }

    private uint StencilImageId => _sharedContext.StencilImageId;
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
    }

    public void Configure(IGraphConfig config)
    {
        config.WriteImage(StencilImageId, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, in IExecutionContext ctx)
    {
        using var commandContext = ctx.UsingCmd();
        var cmd = commandContext.Get();
        
        var image = graph.GetImageOrException(StencilImageId);
        cmd.ClearStencilImages(0, ImageLayout.General, image);
    }
}