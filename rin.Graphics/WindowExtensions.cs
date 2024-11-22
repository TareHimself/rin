using rin.Graphics.Windows;


namespace rin.Graphics;

public static class WindowExtensions
{
    public static WindowRenderer? GetRenderer(this Window window)
    {
        return SGraphicsModule.Get().GetWindowRenderer(window);
    }
}