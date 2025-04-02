using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;

namespace Rin.Engine.Core.Math;

public struct Vector2<T>(T inX, T inY) : ICloneable<Vector2<T>>,
    IFormattable,
    IAdditionOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    IAdditionOperators<Vector2<T>, T, Vector2<T>>,
    ISubtractionOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    ISubtractionOperators<Vector2<T>, T, Vector2<T>>,
    IMultiplyOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    IMultiplyOperators<Vector2<T>, T, Vector2<T>>,
    IDivisionOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    IDivisionOperators<Vector2<T>, T, Vector2<T>>,
    IComparisonOperators<Vector2<T>, Vector2<T>, bool>,
    IVec<Vector2<T>, T>, IEquatable<Vector2<T>> where T : notnull, IComparisonOperators<T, T, bool>

{
    public bool Equals(Vector2<T> other)
    {
        return EqualityComparer<T>.Default.Equals(X, other.X) && EqualityComparer<T>.Default.Equals(Y, other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        if (X is IFormattable xFormat && Y is IFormattable yFormat)
            return
                $"<{xFormat.ToString(format, formatProvider)}{separator} {yFormat.ToString(format, formatProvider)}>";

        return
            $"<{X}{separator} {Y}>";
    }

    public T X = inX;
    public T Y = inY;

    public Vector2(T data) : this(data, data)
    {
    }

    public static Vector2<T> operator +(Vector2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vector2<T>(lx + r, ly + r);
    }

    public static Vector2<T> operator +(Vector2<T> left, Vector2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vector2<T>(lx + rx, ly + ry);
    }

    public static Vector2<T> operator /(Vector2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vector2<T>(lx / r, ly / r);
    }

    public static Vector2<T> operator /(Vector2<T> left, Vector2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vector2<T>(lx / rx, ly / ry);
    }

    public static Vector2<T> operator *(Vector2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vector2<T>(lx * r, ly * r);
    }

    public static Vector2<T> operator *(Vector2<T> left, Vector2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vector2<T>(lx * rx, ly * ry);
    }

    public static Vector2<T> operator -(Vector2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vector2<T>(lx - r, ly - r);
    }

    public static Vector2<T> operator -(Vector2<T> left, Vector2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vector2<T>(lx - rx, ly - ry);
    }

    public bool Within(Vector2<T> p1, Vector2<T> p2)
    {
        return p1 <= this && this <= p2;
    }

    public bool Within(Pair<Vector2<T>, Vector2<T>> bounds)
    {
        var (p1, p2) = bounds;
        return p1 <= this && this <= p2;
    }

    public Vector2<T> Clone()
    {
        return new Vector2<T>(X, Y);
    }

    public Vector2<T> Clamp(Vector2<T> min, Vector2<T> max)
    {
        return new Vector2<T>(X < min.X ? min.X : X > max.X ? max.X : X, Y < min.X ? min.Y : Y > max.Y ? max.Y : Y);
    }

    public Vector2<T> Clamp(T min, T max)
    {
        return Clamp(new Vector2<T>(min), new Vector2<T>(max));
    }

    [Pure]
    public Vector2<E> Cast<E>() where E : notnull, IComparisonOperators<E, E, bool>
    {
        dynamic a = X, b = Y;

        return new Vector2<E>((E)a, (E)b);
    }

    public T Dot(Vector2<T> other)
    {
        dynamic ax = X, ay = Y, bx = other.X, by = other.Y;
        return ax * bx + ay * by;
    }

    public double Length()
    {
        dynamic ax = X, ay = Y;
        return System.Math.Sqrt(ax * ax + ay * ay);
    }

    public double Acos(Vector2<T> other)
    {
        dynamic dot = Dot(other);

        var mul = Length() * other.Length();
        // Calculate the cosine of the angle between the vectors
        var cosine = mul == 0 ? 0 : (double)dot / mul;

        // Calculate the angle in radians using arccosine
        return System.Math.Acos(cosine);
    }

    public double Acosd(Vector2<T> other)
    {
        return Acos(other) * System.Math.PI / 180.0f;
    }

    public double Cross(Vector2<T> other)
    {
        dynamic ux = X, uy = Y, vx = other.X, vy = other.Y;
        return ux * vy - uy * vx;
    }

    public static implicit operator Vector2<T>(T data)
    {
        return new Vector2<T>(data);
    }

    public static bool operator ==(Vector2<T> left, Vector2<T> right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    public static bool operator !=(Vector2<T> left, Vector2<T> right)
    {
        return !(left == right);
    }

    public static bool operator >(Vector2<T> left, Vector2<T> right)
    {
        return left.X > right.X && left.Y > right.Y;
    }

    public static bool operator >=(Vector2<T> left, Vector2<T> right)
    {
        return left.X >= right.X && left.Y >= right.Y;
    }

    public static bool operator <(Vector2<T> left, Vector2<T> right)
    {
        return left.X < right.X && left.Y < right.Y;
    }

    public static bool operator <=(Vector2<T> left, Vector2<T> right)
    {
        return left.X <= right.X && left.Y <= right.Y;
    }

    public void Deconstruct(out T x, out T y)
    {
        x = X;
        y = Y;
    }
}

/*
public class Vector2f : Vector2<float>
{

    public Vector2f() : base(0)
    {

    }

    public Vector2f(float data) : base(data)
    {
    }

    public Vector2f(float inX, float inY) : base(inX, inY)
    {
    }
}

public class Vector2d : Vector2<double>
{

    public Vector2d() : base(0)
    {

    }

    public Vector2d(double data) : base(data)
    {
    }

    public Vector2d(double inX, double inY) : base(inX, inY)
    {
    }

    public static implicit operator Vector2d(Vector2<double> src)
    {
        return new Vector2();
    }
}

public class Vector2i : Vector2<int>
{

    public Vector2i() : base(0)
    {

    }

    public Vector2i(int data) : base(data)
    {
    }

    public Vector2i(int inX, int inY) : base(inX, inY)
    {
    }
}*/