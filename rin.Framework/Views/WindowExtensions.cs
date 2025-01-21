using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Graphics;


namespace rin.Framework.Views;

public static class WindowExtensions
{
    public static WindowSurface? GetViewSurface(this IWindow window)
    {
        return SViewsModule.Get().GetWindowSurface(window);
    }
}