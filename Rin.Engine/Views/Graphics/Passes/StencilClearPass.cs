using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
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
        config.UseImage(StencilImageId,ImageLayout.StencilAttachment,ResourceUsage.Write);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var image = graph.GetImageOrException(StencilImageId);
        
        var cmd = frame.GetCommandBuffer();
        cmd.ClearStencilImages(0, image.Layout, image);
    }
}