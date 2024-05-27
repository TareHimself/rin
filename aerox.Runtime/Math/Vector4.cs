using System.Numerics;
using System.Runtime.InteropServices;

namespace aerox.Runtime.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vector4<T> :
    IAdditionOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    IAdditionOperators<Vector4<T>, T, Vector4<T>>,
    ISubtractionOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    ISubtractionOperators<Vector4<T>, T, Vector4<T>>,
    IMultiplyOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    IMultiplyOperators<Vector4<T>, T, Vector4<T>>,
    IDivisionOperators<Vector4<T>, Vector4<T>, Vector4<T>>,
    IDivisionOperators<Vector4<T>, T, Vector4<T>>,
    ICloneable<Vector4<T>>
    where T : notnull
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

    public Vector4<T> Clone()
    {
        return new Vector4<T>(X, Y, Z, W);
    }

    public static implicit operator Vector4<T>(T data)
    {
        return new Vector4<T>(data);
    }
}