using JetBrains.Annotations;

namespace Rin.Framework.Graphics;

public record struct WindowRect
{
    [PublicAPI] public Extent2D Extent;

    [PublicAPI] public Point2D Point;

    public void Deconstruct(out Point2D point, out Extent2D extent)
    {
        point = Point;
        extent = Extent;
    }
}