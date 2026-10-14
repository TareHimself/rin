using Rin.Core.Graphics;

namespace Examples.StencilClip;

public sealed class StencilClipExample : Example
{
    public override string Name => "stencil-clip";

    public override string Title => "Stencil Clip";

    public override Extent2D WindowSize => new(900, 620);

    public override void Start(ExampleContext context)
    {
        context.Add(StencilScene.Build());
    }
}
