using aerox.Runtime.Windows;

namespace aerox.Runtime.Graphics;

public static class WindowExtensions
{
    public static WindowRenderer? GetRenderer(this Window window)
    {
        return SGraphicsModule.Get().GetWindowRenderer(window);
    }
}