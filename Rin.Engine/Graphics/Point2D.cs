using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics;

public record struct Point2D : IFormattable
{
    
    [PublicAPI] public int X = 0;
    
    [PublicAPI] public int Y = 0;
    
    public Point2D()
    {
    }

    public Point2D(int x, int y)
    {
        X = x;
        Y = y;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
    }

    
    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}