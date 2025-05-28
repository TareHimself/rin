using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Window;

namespace Rin.Engine.Views;

public static class WindowExtensions
{
    public static WindowSurface? GetViewSurface(this IWindow window)
    {
        return SViewsModule.Get().GetWindowSurface(window);
    }
}