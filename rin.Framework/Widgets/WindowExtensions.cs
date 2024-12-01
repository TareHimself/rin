using rin.Framework.Graphics.Windows;
using rin.Framework.Widgets.Graphics;


namespace rin.Framework.Widgets;

public static class WindowExtensions
{
    public static WindowSurface? GetWidgetSurface(this IWindow window)
    {
        return SWidgetsModule.Get().GetWindowSurface(window);
    }
}