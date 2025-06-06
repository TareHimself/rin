using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics;

public record struct Extent2D : IFormattable
{
    [PublicAPI] public uint Width = 0;
    
    [PublicAPI] public uint Height = 0;

    public Extent2D()
    {
    }

    public Extent2D(uint width, uint height)
    {
        Width = width;
        Height = height;
    }

    public Extent2D(int width, int height)
    {
        Width = (uint)width;
        Height = (uint)height;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{Width.ToString(format, formatProvider)}{separator} {Height.ToString(format, formatProvider)}>";
    }

    public void Deconstruct(out uint width, out uint height)
    {
        width = Width;
        height = Height;
    }
}