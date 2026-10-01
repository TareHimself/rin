using Examples.Common;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;

namespace UiGallery;

public class UiGalleryApplication : ExampleApplication
{
    protected override void OnStartup()
    {
        IGraphicsModule.Get().OnWindowRendererCreated += UiGalleryScene.Create;
        IGraphicsModule.Get().OnWindowCreated += window => window.OnClose += _ => RequestExit();
        IGraphicsModule.Get().CreateWindow("UI Gallery", new Extent2D(1120, 760),
            WindowFlags.Visible | WindowFlags.Resizable);
    }
}
