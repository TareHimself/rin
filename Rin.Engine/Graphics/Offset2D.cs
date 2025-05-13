using System.Globalization;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine.Graphics;

public struct Offset2D : IEquatable<Offset2D>, IFormattable
{
    public bool Equals(Offset2D other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Offset2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
    }

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


    public bool Equals(Vector2<uint> other)
    {
        return X == other.X && Y == other.Y;
    }

    public static bool operator ==(Offset2D left, Vector2<uint> right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    public static bool operator !=(Offset2D left, Vector2<uint> right)
    {
        return left.X != right.X || left.Y != right.Y;
    }

    public static implicit operator Offset2D(Vector2<uint> target)
    {
        return new Offset2D
        {
            Y = target.Y,
            X = target.X
        };
    }

    public void Deconstruct(out uint x, out uint y)
    {
        x = X;
        y = Y;
    }
}