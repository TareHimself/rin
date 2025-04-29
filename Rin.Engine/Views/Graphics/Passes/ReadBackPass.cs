using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics.Passes;

public class ReadBackPass(SharedPassContext sharedContext) : IViewsPass
{

    private uint MainImageId => sharedContext.MainImageId;
    private uint CopyImageId => sharedContext.CopyImageId;
    private uint StencilImageId => sharedContext.StencilImageId;
    public uint Id { get; set; }
    public bool IsTerminal { get; } = false;
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
        config.ReadImage(MainImageId,ImageLayout.TransferSrc);
        config.WriteImage(CopyImageId,ImageLayout.TransferDst);
    }

    public void Execute(ICompiledGraph graph, Frame frame, IRenderContext context)
    {
        var mainImage = graph.GetImageOrException(MainImageId);
        var copyImage = graph.GetImageOrException(CopyImageId);

        var cmd = frame.GetCommandBuffer();

        cmd
            .CopyImageToImage(mainImage, copyImage);
    }

    public static IViewsPass Create(PassCreateInfo info)
    {
        return new ReadBackPass(info.Context);
    }
}