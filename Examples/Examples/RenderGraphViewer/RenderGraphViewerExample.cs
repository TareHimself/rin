using Examples.Common;
using Examples.RenderGraphOverlay;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;

namespace Examples.RenderGraphViewer;

public sealed class RenderGraphViewerExample : Example
{
    public override string Name => "render-graph";

    public override string Title => "Render Graph Viewer";

    public override Extent2D WindowSize => new(1400, 800);

    public override void Start(ExampleContext context)
    {
        context.Add(new RectView
        {
            Color = Color.FromRgb(0.16f, 0.2f, 0.3f),
            Padding = new Padding(40f),
            InitChild = new TextBoxView
            {
                Content = "Something for the overlay to look at",
                FontSize = 28f
            }
        });
        context.Add(GraphOverlay.Create(context.Surface, true));
    }
}
