using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Window;

namespace Rin.Framework.Views;

public static class WindowExtensions
{
    public static WindowSurface? GetViewSurface(this IWindow window)
    {
        return SViewsModule.Get().GetWindowSurface(window);
    }
}