using Examples.ImageStrip.Views;
using Rin.Core.Graphics;

namespace Examples.ImageStrip;

public sealed class ImageStripExample : Example
{
    public override string Name => "image-strip";

    public override string Title => "Image Strip";

    public override Extent2D WindowSize => new(1280, 720);

    public override void Start(ExampleContext context)
    {
        var view = context.Add(new ImageStripView());
        context.OnDrop(e => view.AddImages(e.Paths));
    }
}
