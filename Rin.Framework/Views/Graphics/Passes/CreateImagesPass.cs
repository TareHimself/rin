using System.Numerics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;

namespace Rin.Framework.Views.Graphics.Passes;

public class CreateImagesPass : IPass
{
    public CreateImagesPass(SurfaceContext context)
    {
        Context = context;
    }

    public SurfaceContext Context { get; set; }

    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        Context.MainImageId = config.CreateTexture(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.CopyImageId = config.CreateTexture(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.StencilImageId = config.CreateTexture(Context.Extent, ImageFormat.Stencil, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var drawImage = graph.GetTexture(Context.MainImageId);
        var copyImage = graph.GetTexture(Context.CopyImageId);
        var stencilImage = graph.GetTexture(Context.StencilImageId);

        ctx
            .ClearColorImages(new Vector4(0.0f), drawImage, copyImage)
            .ClearStencilImages(0, stencilImage);
    }
}