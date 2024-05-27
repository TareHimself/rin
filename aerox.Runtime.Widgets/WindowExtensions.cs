using aerox.Runtime.Windows;

namespace aerox.Runtime.Widgets;

public static class WindowExtensions
{
    public static WidgetWindowSurface? GetWidgetSurface(this Window window)
    {
        return SWidgetsModule.Get().GetWindowSurface(window);
    }
}