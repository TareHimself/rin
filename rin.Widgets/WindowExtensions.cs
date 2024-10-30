using rin.Widgets.Graphics;
using rin.Windows;

namespace rin.Widgets;

public static class WindowExtensions
{
    public static WindowSurface? GetWidgetSurface(this Window window)
    {
        return SWidgetsModule.Get().GetWindowSurface(window);
    }
}