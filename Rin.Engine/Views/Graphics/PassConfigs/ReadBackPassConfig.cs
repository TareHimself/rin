using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics.PassConfigs;

public class ReadBackPassConfig : IPassConfig
{
    private uint MainImageId => PassContext.MainImageId;
    private uint CopyImageId => PassContext.CopyImageId;
    public uint Id { get; set; }
    public bool IsTerminal { get; } = false;

    public SurfacePassContext PassContext { get; init; } = null!;

    public void Configure(IGraphConfig config)
    {
        config.ReadImage(MainImageId, ImageLayout.TransferSrc);
        config.WriteImage(CopyImageId, ImageLayout.TransferDst);
    }

    public void Begin(ICompiledGraph graph, IExecutionContext ctx)
    {
        
    }

    public void End(ICompiledGraph graph, IExecutionContext ctx)
    {
        
    }
    
    // public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    // {
    //     var mainImage = graph.GetImageOrException(MainImageId);
    //     var copyImage = graph.GetImageOrException(CopyImageId);
    //     ctx.CopyToImage(mainImage, copyImage);
    // }
}