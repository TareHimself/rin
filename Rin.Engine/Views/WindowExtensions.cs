using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views;

public static class WindowExtensions
{
    public static WindowSurface? GetViewSurface(this IWindow window)
    {
        return SViewsModule.Get().GetWindowSurface(window);
    }
}