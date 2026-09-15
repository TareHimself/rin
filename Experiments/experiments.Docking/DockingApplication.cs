using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Window;
using Examples.Common;
using experiments.Docking.Demo;
using experiments.Docking.Views;

namespace experiments.Docking;

/// <summary>
///     Demo host for the docking system. Opens one window with an initial docked layout;
///     tabs can be dragged to redock, splitters dragged to resize, tabs closed with "×".
/// </summary>
public class DockingApplication : ExampleApplication
{
    private DockSpaceView? _dockSpace;

    protected override void OnStartup()
    {
        IGraphicsModule.Get().OnWindowCreated += window =>
        {
            window.OnClose += _ => RequestExit();
            window.OnKey += e =>
            {
                if (e is { Key: InputKey.R, State: InputState.Pressed })
                    _dockSpace?.ResetTo(DemoPanels.BuildInitialTree());
            };
        };

        IViewsModule.Get().OnSurfaceCreated += OnSurfaceCreated;

        IGraphicsModule.Get()
            .CreateWindow("Rin Docking", new Extent2D(1280, 800), WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown()
    {
    }

    private void OnSurfaceCreated(IWindowSurface surface)
    {
        _dockSpace = new DockSpaceView(DemoPanels.BuildInitialTree());
        surface.Add(_dockSpace);
    }
}
