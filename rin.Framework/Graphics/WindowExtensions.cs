using rin.Framework.Graphics.Windows;

namespace rin.Framework.Graphics;

public static class WindowExtensions
{
    public static IWindowRenderer? GetRenderer(this IWindow window)
    {
        return SGraphicsModule.Get().GetWindowRenderer(window);
    }
}