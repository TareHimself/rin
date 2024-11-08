using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;

namespace rin.Core.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vector2<T>(T inX, T inY) : ICloneable<Vector2<T>>,
    IAdditionOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    IAdditionOperators<Vector2<T>, T, Vector2<T>>,
    ISubtractionOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    ISubtractionOperators<Vector2<T>, T, Vector2<T>>,
    IMultiplyOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    IMultiplyOperators<Vector2<T>, T, Vector2<T>>,
    IDivisionOperators<Vector2<T>, Vector2<T>, Vector2<T>>,
    IDivisionOperators<Vector2<T>, T, Vector2<T>>
    where T : notnull, IComparisonOperators<T,T,bool>

{
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
        dynamic p1x = p1.X, p1y = p1.Y, p2x = p2.X, p2y = p2.Y;
        var isWithinHorizontal = p1x < X && X < p2x;
        var isWithinVertical = p1y < Y && Y < p2y;
        return isWithinHorizontal && isWithinVertical;
    }

    public bool Within(Pair<Vector2<T>, Vector2<T>> bounds)
    {
        dynamic p1x = bounds.First.X, p1y = bounds.First.Y, p2x = bounds.Second.X, p2y = bounds.Second.Y;
        var isWithinHorizontal = p1x < X && X < p2x;
        var isWithinVertical = p1y < Y && Y < p2y;
        return isWithinHorizontal && isWithinVertical;
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
        return this.Clamp(new Vector2<T>(min),new Vector2<T>(max));
    }

    [Pure]
    public Vector2<E> Cast<E>() where E : notnull, IComparisonOperators<E,E,bool>
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
        dynamic ux = X,uy = Y,vx = other.X,vy = other.Y;
        return ux * vy - uy * vx;
    }
    
    public static implicit operator Vector2<T>(T data)
    {
        return new Vector2<T>(data);
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