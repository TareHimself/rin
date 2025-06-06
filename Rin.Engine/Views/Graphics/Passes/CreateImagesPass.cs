using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.Views.Graphics.Passes;

public class CreateImagesPass : IPass
{
    public CreateImagesPass(SharedPassContext context)
    {
        Context = context;
    }

    public SharedPassContext Context { get; set; }

    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        Context.MainImageId = config.CreateImage(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.CopyImageId = config.CreateImage(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.StencilImageId = config.CreateImage(Context.Extent, ImageFormat.Stencil, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var drawImage = graph.GetImage(Context.MainImageId);
        var copyImage = graph.GetImage(Context.CopyImageId);
        var stencilImage = graph.GetImage(Context.StencilImageId);

        ctx
            .ClearColorImages(new Vector4(0.0f), ImageLayout.General, drawImage, copyImage)
            .ClearStencilImages(0, ImageLayout.General, stencilImage);
    }
}