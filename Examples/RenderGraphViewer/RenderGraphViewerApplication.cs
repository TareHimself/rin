using System.Numerics;
using Examples.Common;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using RenderGraphOverlay;

namespace RenderGraphViewer;

public class RenderGraphViewerApplication : ExampleApplication
{
    protected override void OnStartup()
    {
        IGraphicsModule.Get().OnWindowRendererCreated += CreateScene;
        IGraphicsModule.Get().OnWindowCreated += window => window.OnClose += _ => RequestExit();
        IGraphicsModule.Get().CreateWindow("Render Graph Viewer", new Extent2D(1400, 800),
            WindowFlags.Visible | WindowFlags.Resizable);
    }

    private static void CreateScene(IWindowRenderer renderer)
    {
        if (IViewsModule.Get().GetWindowSurface(renderer) is not { } surface) return;

        surface.Add(new RectView
        {
            Color = new Color(0.16f, 0.2f, 0.3f, 1f),
            Padding = new Padding(40f),
            InitChild = new TextBoxView
            {
                Content = "Something for the overlay to look at",
                FontSize = 28f
            }
        });
        GraphOverlay.Attach(surface, startOpen: true);
    }
}
