﻿using System.Globalization;
using JetBrains.Annotations;

namespace Rin.Engine.Graphics;

public record struct Extent3D : IFormattable
{
    [PublicAPI] public uint Dimensions = 1;
    [PublicAPI] public uint Height = 0;

    [PublicAPI] public uint Width = 0;

    public Extent3D()
    {
    }

    public Extent3D(in Extent2D extent, uint dimensions = 1)
    {
        Width = extent.Width;
        Height = extent.Height;
        Dimensions = dimensions;
    }

    public Extent3D(uint width, uint height, uint dimensions = 1)
    {
        Width = width;
        Height = height;
        Dimensions = dimensions;
    }

    public Extent3D(int width, int height, int dimensions = 1)
    {
        Width = (uint)width;
        Height = (uint)height;
        Dimensions = (uint)dimensions;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{Width.ToString(format, formatProvider)}{separator} {Height.ToString(format, formatProvider)}{separator} {Dimensions.ToString(format, formatProvider)}>";
    }


    public static implicit operator Extent2D(Extent3D extent)
    {
        return new Extent2D(extent.Width, extent.Height);
    }
}