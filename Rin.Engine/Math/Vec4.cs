using System.Numerics;

namespace Rin.Engine.Math;

public struct Vec4<T> :
    IAdditionOperators<Vec4<T>, Vec4<T>, Vec4<T>>,
    IAdditionOperators<Vec4<T>, T, Vec4<T>>,
    ISubtractionOperators<Vec4<T>, Vec4<T>, Vec4<T>>,
    ISubtractionOperators<Vec4<T>, T, Vec4<T>>,
    IMultiplyOperators<Vec4<T>, Vec4<T>, Vec4<T>>,
    IMultiplyOperators<Vec4<T>, T, Vec4<T>>,
    IDivisionOperators<Vec4<T>, Vec4<T>, Vec4<T>>,
    IDivisionOperators<Vec4<T>, T, Vec4<T>>,
    IVec<Vec4<T>, T>
    where T : notnull, IComparisonOperators<T, T, bool>, IUnaryNegationOperators<T, T>
{
    public T X;
    public T Y;
    public T Z;
    public T W;

    public Vec4(T data)
    {
        X = data;
        Y = data;
        Z = data;
        W = data;
    }

    public Vec4(T inX, T inY, T inZ, T inW)
    {
        X = inX;
        Y = inY;
        Z = inZ;
        W = inW;
    }

    public Vec4(Vector2<T> xy, Vector2<T> zw)
    {
        X = xy.X;
        Y = xy.Y;
        Z = zw.X;
        W = zw.Y;
    }

    public Vec4(T xy, T zw)
    {
        X = xy;
        Y = xy;
        Z = zw;
        W = zw;
    }

    public Vec4(Vec3<T> vec3, T inW)
    {
        X = vec3.X;
        Y = vec3.Y;
        Z = vec3.Z;
        W = inW;
    }

    public static Vec4<T> operator +(Vec4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vec4<T>(lx + r, ly + r, lz + r, lw + r);
    }

    public static Vec4<T> operator +(Vec4<T> left, Vec4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vec4<T>(lx + rx, ly + ry, lz + rz, lw + rw);
    }

    public static Vec4<T> operator /(Vec4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vec4<T>(lx / r, ly / r, lz / r, lw / r);
    }

    public static Vec4<T> operator /(Vec4<T> left, Vec4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vec4<T>(lx / rx, ly / ry, lz / rz, lw / rw);
    }

    public static Vec4<T> operator *(Vec4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vec4<T>(lx * r, ly * r, lz * r, lw * r);
    }

    public static Vec4<T> operator *(Vec4<T> left, Vec4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vec4<T>(lx * rx, ly * ry, lz * rz, lw * rw);
    }

    public static Vec4<T> operator -(Vec4<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, lw = left.W, r = right;

        return new Vec4<T>(lx - r, ly - r, lz - r, lw - r);
    }

    public static Vec4<T> operator -(Vec4<T> left, Vec4<T> right)
    {
        dynamic lx = left.X,
            ly = left.Y,
            lz = left.Z,
            lw = left.W,
            rx = right.X,
            ry = right.Y,
            rz = right.Z,
            rw = right.W;

        return new Vec4<T>(lx - rx, ly - ry, lz - rz, lw - rw);
    }

    public Vec4<T> Clone()
    {
        return new Vec4<T>(X, Y, Z, W);
    }

    public static implicit operator Vec4<T>(T data)
    {
        return new Vec4<T>(data);
    }

    public void Deconstruct(out T x, out T y, out T z, out T w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }
}