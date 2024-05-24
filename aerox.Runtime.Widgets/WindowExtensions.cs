using aerox.Runtime.Windows;

namespace aerox.Runtime.Widgets;

public static class WindowExtensions
{
    public static WidgetWindowSurface? GetWidgetSurface(this Window window) =>
        WidgetsModule.Get().GetWindowSurface(window);
}