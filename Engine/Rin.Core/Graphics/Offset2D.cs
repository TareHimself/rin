using System.Globalization;
using JetBrains.Annotations;

namespace Rin.Core.Graphics;

[NoReorder]
public record struct Offset2D : IFormattable
{
    [PublicAPI] public uint X = 0;
    [PublicAPI] public uint Y = 0;

    public Offset2D()
    {
    }

    public Offset2D(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    public Offset2D(int x, int y)
    {
        X = (uint)x;
        Y = (uint)y;
    }

    public static Offset2D Zero => new(0, 0);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
    }

    public void Deconstruct(out uint x, out uint y)
    {
        x = X;
        y = Y;
    }
}