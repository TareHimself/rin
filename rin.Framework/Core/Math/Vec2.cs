using System.Collections;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;

namespace rin.Framework.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vec2<T>(T inX, T inY) : ICloneable<Vec2<T>>,
    IAdditionOperators<Vec2<T>, Vec2<T>, Vec2<T>>,
    IAdditionOperators<Vec2<T>, T, Vec2<T>>,
    ISubtractionOperators<Vec2<T>, Vec2<T>, Vec2<T>>,
    ISubtractionOperators<Vec2<T>, T, Vec2<T>>,
    IMultiplyOperators<Vec2<T>, Vec2<T>, Vec2<T>>,
    IMultiplyOperators<Vec2<T>, T, Vec2<T>>,
    IDivisionOperators<Vec2<T>, Vec2<T>, Vec2<T>>,
    IDivisionOperators<Vec2<T>, T, Vec2<T>>,
    IComparisonOperators<Vec2<T>,Vec2<T>,bool>,

IVec<Vec2<T>,T>, IEquatable<Vec2<T>> where T : notnull, IComparisonOperators<T,T,bool>

{
    public bool Equals(Vec2<T> other)
    {
        return EqualityComparer<T>.Default.Equals(X, other.X) && EqualityComparer<T>.Default.Equals(Y, other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vec2<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public T X = inX;
    public T Y = inY;

    public Vec2(T data) : this(data, data)
    {
    }

    public static Vec2<T> operator +(Vec2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vec2<T>(lx + r, ly + r);
    }

    public static Vec2<T> operator +(Vec2<T> left, Vec2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vec2<T>(lx + rx, ly + ry);
    }

    public static Vec2<T> operator /(Vec2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vec2<T>(lx / r, ly / r);
    }

    public static Vec2<T> operator /(Vec2<T> left, Vec2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vec2<T>(lx / rx, ly / ry);
    }

    public static Vec2<T> operator *(Vec2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vec2<T>(lx * r, ly * r);
    }

    public static Vec2<T> operator *(Vec2<T> left, Vec2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vec2<T>(lx * rx, ly * ry);
    }

    public static Vec2<T> operator -(Vec2<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, r = right;

        return new Vec2<T>(lx - r, ly - r);
    }

    public static Vec2<T> operator -(Vec2<T> left, Vec2<T> right)
    {
        dynamic lx = left.X, ly = left.Y, rx = right.X, ry = right.Y;

        return new Vec2<T>(lx - rx, ly - ry);
    }

    public bool Within(Vec2<T> p1, Vec2<T> p2)
    {
        return p1 <= this && this <= p2;
    }

    public bool Within(Pair<Vec2<T>, Vec2<T>> bounds)
    {
        var (p1, p2) = bounds;
        return p1 <= this && this <= p2;
    }

    public Vec2<T> Clone()
    {
        return new Vec2<T>(X, Y);
    }
    
    public Vec2<T> Clamp(Vec2<T> min, Vec2<T> max)
    {
        return new Vec2<T>(X < min.X ? min.X : X > max.X ? max.X : X, Y < min.X ? min.Y : Y > max.Y ? max.Y : Y);
    }
    
    public Vec2<T> Clamp(T min, T max)
    {
        return this.Clamp(new Vec2<T>(min),new Vec2<T>(max));
    }

    [Pure]
    public Vec2<E> Cast<E>() where E : notnull, IComparisonOperators<E,E,bool>
    {
        dynamic a = X, b = Y;

        return new Vec2<E>((E)a, (E)b);
    }

    public T Dot(Vec2<T> other)
    {
        dynamic ax = X, ay = Y, bx = other.X, by = other.Y;
        return ax * bx + ay * by;
    }

    public double Length()
    {
        dynamic ax = X, ay = Y;
        return System.Math.Sqrt(ax * ax + ay * ay);
    }

    public double Acos(Vec2<T> other)
    {
        dynamic dot = Dot(other);

        var mul = Length() * other.Length();
        // Calculate the cosine of the angle between the vectors
        var cosine = mul == 0 ? 0 : (double)dot / mul;

        // Calculate the angle in radians using arccosine
        return System.Math.Acos(cosine);
    }

    public double Acosd(Vec2<T> other)
    {
        return Acos(other) * System.Math.PI / 180.0f;
    }
    
    public double Cross(Vec2<T> other)
    {
        dynamic ux = X,uy = Y,vx = other.X,vy = other.Y;
        return ux * vy - uy * vx;
    }
    
    public static implicit operator Vec2<T>(T data)
    {
        return new Vec2<T>(data);
    }

    public IEnumerator<T> GetEnumerator() => new ParamsEnumerator<T>(X, Y);

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static bool operator ==(Vec2<T> left, Vec2<T> right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    public static bool operator !=(Vec2<T> left, Vec2<T> right)
    {
        return !(left == right);
    }

    public static bool operator >(Vec2<T> left, Vec2<T> right)
    {
        return left.X > right.X && left.Y > right.Y;
    }

    public static bool operator >=(Vec2<T> left, Vec2<T> right)
    {
        return left.X >= right.X && left.Y >= right.Y;
    }

    public static bool operator <(Vec2<T> left, Vec2<T> right)
    {
        return left.X < right.X && left.Y < right.Y;
    }

    public static bool operator <=(Vec2<T> left, Vec2<T> right)
    {
        return left.X <= right.X && left.Y <= right.Y;
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