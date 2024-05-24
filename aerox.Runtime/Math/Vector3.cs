using System.Numerics;
using System.Runtime.InteropServices;
using MathNet.Numerics.LinearAlgebra;

namespace aerox.Runtime.Math;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vector3<T>(T inX, T inY, T inZ) :
    IAdditionOperators<Vector3<T>, Vector3<T>, Vector3<T>>,
    IAdditionOperators<Vector3<T>, T, Vector3<T>>,
    ISubtractionOperators<Vector3<T>, Vector3<T>, Vector3<T>>,
    ISubtractionOperators<Vector3<T>, T, Vector3<T>>,
    IMultiplyOperators<Vector3<T>, Vector3<T>, Vector3<T>>,
    IMultiplyOperators<Vector3<T>, T, Vector3<T>>,
    IDivisionOperators<Vector3<T>, Vector3<T>, Vector3<T>>,
    IDivisionOperators<Vector3<T>, T, Vector3<T>>,
    ICloneable<Vector3<T>>
    where T : notnull
{
    public T X = inX;
    public T Y = inY;
    public T Z = inZ;

    public Vector3(T data) : this(data, data, data)
    {
    }

    public static Vector3<T> operator +(Vector3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vector3<T>(lx + r, ly + r, lz + r);
    }

    public static Vector3<T> operator +(Vector3<T> left, Vector3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vector3<T>(lx + rx, ly + ry, lz + rz);
    }

    public static Vector3<T> operator /(Vector3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vector3<T>(lx / r, ly / r, lz / r);
    }

    public static Vector3<T> operator /(Vector3<T> left, Vector3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vector3<T>(lx / rx, ly / ry, lz / rz);
    }

    public static Vector3<T> operator *(Vector3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vector3<T>(lx * r, ly * r, lz * r);
    }

    public static Vector3<T> operator *(Vector3<T> left, Vector3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vector3<T>(lx * rx, ly * ry, lz * rz);
    }

    public static Vector3<T> operator -(Vector3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vector3<T>(lx - r, ly - r, lz - r);
    }

    public static Vector3<T> operator -(Vector3<T> left, Vector3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vector3<T>(lx - rx, ly - ry, lz - rz);
    }

    public Vector3<T> Cross(Vector3<T> other)
    {
        dynamic lx = X, ly = Y, lz = Z, rx = other.X, ry = other.Y, rz = other.Z;

        return new Vector3<T>(ly * rz - ry * lz, lz * rx - rz * lx, lx * ry - rx * ly);
    }
    
    public static Vector3<T> Zero
    {
        get
        {
            dynamic a = 0, b = 0, c = 0;
            return new Vector3<T>((T)a, (T)b, (T)c);
        }
    }

    public static Vector3<T> Up{
        get
        {
            dynamic x = 0, y = 1, z = 0;
            return new Vector3<T>((T)x, (T)y, (T)z);
        }
    }
    
    public static Vector3<T> Forward{
        get
        {
            dynamic x = 0, y = 0, z = 1;
            return new Vector3<T>((T)x, (T)y, (T)z);
        }
    }
    
    public static Vector3<T> Right{
        get
        {
            dynamic x = 1, y = 0, z = 0;
            return new Vector3<T>((T)x, (T)y, (T)z);
        }
    }
    
    public static Vector3<T> Unit{
        get
        {
            dynamic x = 1, y = 1, z = 1;
            return new Vector3<T>((T)x, (T)y, (T)z);
        }
    }

    public Vector3<T> Clone() => new Vector3<T>(X, Y, Z);
    
}

