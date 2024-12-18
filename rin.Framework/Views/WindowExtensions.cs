using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Graphics;


namespace rin.Framework.Views;

public static class WindowExtensions
{
    public static WindowSurface? GetWidgetSurface(this IWindow window)
    {
        return SWidgetsModule.Get().GetWindowSurface(window);
    }
}