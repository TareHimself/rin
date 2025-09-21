using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Window;

namespace Rin.Framework.Views;

public static class WindowExtensions
{
    public static IWindowSurface? GetViewSurface(this IWindow window)
    {
        return IViewsModule.Get().GetWindowSurface(window);
    }
}