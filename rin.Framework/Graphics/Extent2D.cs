using System.Numerics;
using JetBrains.Annotations;
using rin.Framework.Core.Math;

namespace rin.Framework.Graphics;

public struct Extent2D : IEqualityOperators<Extent2D, Vec2<uint>, bool>, IEquatable<Extent2D>
{
    public bool Equals(Extent2D other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is Extent2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

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


    public bool Equals(Vec2<uint> other)
    {
        return Width == other.X && Height == other.Y;
    }

    public static bool operator ==(Extent2D left, Vec2<uint> right)
    {
        return left.Width == right.X && left.Height == right.Y;
    }

    public static bool operator !=(Extent2D left, Vec2<uint> right)
    {
        return left.Width != right.X || left.Height != right.Y;
    }

    public static implicit operator Extent2D(Vec2<uint> target)
    {
        return new Extent2D()
        {
            Height = target.Y,
            Width = target.X
        };
    }

    public static implicit operator Vec2<uint>(Extent2D target)
    {
        return new Vec2<uint>()
        {
            X = target.Width,
            Y = target.Height
        };
    }
}