﻿using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics;

public record struct Extent2D : IEqualityOperators<Extent2D, Vector2<uint>, bool>, IFormattable
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

    public static bool operator ==(Extent2D left, Vector2<uint> right)
    {
        return left.Width == right.X && left.Height == right.Y;
    }

    public static bool operator !=(Extent2D left, Vector2<uint> right)
    {
        return left.Width != right.X || left.Height != right.Y;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{Width.ToString(format, formatProvider)}{separator} {Height.ToString(format, formatProvider)}>";
    }


    public bool Equals(Vector2<uint> other)
    {
        return Width == other.X && Height == other.Y;
    }

    public static implicit operator Extent2D(Vector2<uint> target)
    {
        return new Extent2D
        {
            Height = target.Y,
            Width = target.X
        };
    }

    public static implicit operator Vector2<uint>(Extent2D target)
    {
        return new Vector2<uint>
        {
            X = target.Width,
            Y = target.Height
        };
    }

    public void Deconstruct(out uint width, out uint height)
    {
        width = Width;
        height = Height;
    }
}