using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics;

public record struct WindowRect
{
    
    [PublicAPI] public Point2D Point;
    
    [PublicAPI] public Extent2D Extent;
    
    public void Deconstruct(out Point2D point, out Extent2D extent)
    {
        point = Point;
        extent = Extent;
    }
}