using Rin.Engine.Graphics.Windows;

namespace Rin.Engine.Graphics;

public static class WindowExtensions
{
    public static IWindowRenderer? GetRenderer(this IWindow window)
    {
        return SGraphicsModule.Get().GetWindowRenderer(window);
    }
}