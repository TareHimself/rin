using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;

namespace Rin.Core.Views.Graphics.Passes;

public class CreateImagesPass : IPass
{
    public CreateImagesPass(SurfaceContext context)
    {
        Context = context;
    }

    public SurfaceContext Context { get; set; }

    public uint Id { get; set; }
    public bool IsTerminal => false;
    public Action? OnPrune => null;

    public void Configure(IGraphConfig config)
    {
        Context.MainImageId = config.CreateTexture(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.CopyImageId = config.CreateTexture(Context.Extent, ImageFormat.RGBA32, ImageLayout.General);
        Context.StencilImageId = config.CreateTexture(Context.Extent, ImageFormat.Stencil, ImageLayout.General);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var drawImage = graph.GetImage(Context.MainImageId);
        var copyImage = graph.GetImage(Context.CopyImageId);
        var stencilImage = graph.GetImage(Context.StencilImageId);

        ctx
            .ClearColorImages(new Vector4(0.0f), [drawImage, copyImage])
            .ClearStencilImages(0, [stencilImage]);
    }
}