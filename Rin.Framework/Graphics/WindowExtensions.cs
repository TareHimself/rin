using Rin.Framework.Graphics.Windows;

namespace Rin.Framework.Graphics;

public static class WindowExtensions
{
    public static IWindowRenderer? GetRenderer(this IWindow window)
    {
        return SGraphicsModule.Get().GetWindowRenderer(window);
    }
}