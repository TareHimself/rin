using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Window;

namespace Rin.Core.Views;

public static class WindowExtensions
{
    public static IWindowSurface? GetViewSurface(this IWindow window)
    {
        return IViewsModule.Get().GetWindowSurface(window);
    }
}