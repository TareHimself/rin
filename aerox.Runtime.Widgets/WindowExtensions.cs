using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Windows;

namespace aerox.Runtime.Widgets;

public static class WindowExtensions
{
    public static WindowSurface? GetWidgetSurface(this Window window)
    {
        return SWidgetsModule.Get().GetWindowSurface(window);
    }
}