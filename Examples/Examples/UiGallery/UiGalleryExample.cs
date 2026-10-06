using Examples.Common;
using Rin.Core.Graphics;

namespace Examples.UiGallery;

public sealed class UiGalleryExample : Example
{
    public override string Name => "ui-gallery";

    public override string Title => "UI Gallery";

    public override Extent2D WindowSize => new(1120, 760);

    public override void Start(ExampleContext context)
    {
        UiGalleryScene.Create(context.Surface.Renderer);
    }
}
