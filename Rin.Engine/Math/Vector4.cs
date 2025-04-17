using System.Numerics;

namespace Rin.Engine.Math;

public struct Vector4<T> :
    IAdditionOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    IAdditionOperators<Vector4<T>, T, Vector4<T>>,
    ISubtractionOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    ISubtractionOperators<Vector4<T>, T, Vector4<T>>,
    IMultiplyOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    IMultiplyOperators<Vector4<T>, T, Vector4<T>>,
    IDivisionOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    IDivisionOperators<Vector4<T>, T, Vector4<T>>,
    IFormattable
    where T : notnull, IComparisonOperators<T, T, bool>, IUnaryNegationOperators<T, T>
{
    public T X;
    public T Y;
    public T Z;
    public T W;

    public Vector4(T data)
    {
        X = data;
        Y = data;
        Z = data;
        W = data;
    }

    public Vector4(T inX, T inY, T inZ, T inW)
    {
        X = inX;
        Y = inY;
        Z = inZ;
        W = inW;
    }

    public Vector4(Vector2<T> xy, Vector2<T> zw)
    {
        X = xy.X;
        Y = xy.Y;
        Z = zw.X;
        W = zw.Y;
    }

    public Vector4(T xy, T zw)
    {
        X = xy;
        Y = xy;
        Z = zw;
        W = zw;
    }

    public Vector4(Vec3<T> vec3, T inW)
    {
        X = vec3.X;
        Y = vec3.Y;
        Z = vec3.Z;
        W = inW;
    }

    public static Vector4<T> operator +(Vector4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vector4<T>(lx + r, ly + r, lz + r, lw + r);
    }

    public static Vector4<T> operator +(Vector4<T> left, Vector4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vector4<T>(lx + rx, ly + ry, lz + rz, lw + rw);
    }

    public static Vector4<T> operator /(Vector4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vector4<T>(lx / r, ly / r, lz / r, lw / r);
    }

    public static Vector4<T> operator /(Vector4<T> left, Vector4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vector4<T>(lx / rx, ly / ry, lz / rz, lw / rw);
    }

    public static Vector4<T> operator *(Vector4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vector4<T>(lx * r, ly * r, lz * r, lw * r);
    }

    public static Vector4<T> operator *(Vector4<T> left, Vector4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vector4<T>(lx * rx, ly * ry, lz * rz, lw * rw);
    }

    public static Vector4<T> operator -(Vector4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vector4<T>(lx - r, ly - r, lz - r, lw - r);
    }

    public static Vector4<T> operator -(Vector4<T> left, Vector4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vector4<T>(lx - rx, ly - ry, lz - rz, lw - rw);
    }

    public static implicit operator Vector4<T>(T data)
    {
        return new Vector4<T>(data);
    }

    public void Deconstruct(out T x, out T y, out T z, out T w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        FormattableString formattable = $"<{X}, {Y}, {Z}, {W}>";
        return formattable.ToString(formatProvider);
    }

    public override string ToString()
    {
        return $"<{X}, {Y}, {Z}, {W}>";
    }
}