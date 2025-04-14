using System.Numerics;

namespace Rin.Engine.Math;

public struct Vec3<T>(T inX, T inY, T inZ) :
    IAdditionOperators<Vec3<T>, Vec3<T>, Vec3<T>>,
    IAdditionOperators<Vec3<T>, T, Vec3<T>>,
    ISubtractionOperators<Vec3<T>, Vec3<T>, Vec3<T>>,
    ISubtractionOperators<Vec3<T>, T, Vec3<T>>,
    IMultiplyOperators<Vec3<T>, Vec3<T>, Vec3<T>>,
    IMultiplyOperators<Vec3<T>, T, Vec3<T>>,
    IDivisionOperators<Vec3<T>, Vec3<T>, Vec3<T>>,
    IDivisionOperators<Vec3<T>, T, Vec3<T>>,
    IUnaryNegationOperators<Vec3<T>, Vec3<T>>
    where T : notnull, IComparisonOperators<T, T, bool>, IUnaryNegationOperators<T, T>
{
    public T X = inX;
    public T Y = inY;
    public T Z = inZ;

    public Vec3(T data) : this(data, data, data)
    {
    }

    public static Vec3<T> operator +(Vec3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vec3<T>(lx + r, ly + r, lz + r);
    }

    public static Vec3<T> operator +(Vec3<T> left, Vec3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vec3<T>(lx + rx, ly + ry, lz + rz);
    }

    public static Vec3<T> operator /(Vec3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vec3<T>(lx / r, ly / r, lz / r);
    }

    public static Vec3<T> operator /(Vec3<T> left, Vec3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vec3<T>(lx / rx, ly / ry, lz / rz);
    }

    public static Vec3<T> operator *(Vec3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vec3<T>(lx * r, ly * r, lz * r);
    }

    public static Vec3<T> operator *(Vec3<T> left, Vec3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vec3<T>(lx * rx, ly * ry, lz * rz);
    }

    public static Vec3<T> operator -(Vec3<T> left, T right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, r = right;

        return new Vec3<T>(lx - r, ly - r, lz - r);
    }

    public static Vec3<T> operator -(Vec3<T> left, Vec3<T> right)
    {
        dynamic lx = left.X, ly = left.Y, lz = left.Z, rx = right.X, ry = right.Y, rz = right.Z;

        return new Vec3<T>(lx - rx, ly - ry, lz - rz);
    }

    public Vec3<T> Cross(Vec3<T> other)
    {
        dynamic lx = X, ly = Y, lz = Z, rx = other.X, ry = other.Y, rz = other.Z;

        return new Vec3<T>(ly * rz - ry * lz, lz * rx - rz * lx, lx * ry - rx * ly);
    }

    public static Vec3<T> Zero
    {
        get
        {
            dynamic a = 0, b = 0, c = 0;
            return new Vec3<T>((T)a, (T)b, (T)c);
        }
    }

    public static Vec3<T> Up
    {
        get
        {
            dynamic x = 0, y = 1, z = 0;
            return new Vec3<T>((T)x, (T)y, (T)z);
        }
    }

    public static Vec3<T> Forward
    {
        get
        {
            dynamic x = 0, y = 0, z = -1;
            return new Vec3<T>((T)x, (T)y, (T)z);
        }
    }

    public static Vec3<T> Right
    {
        get
        {
            dynamic x = 1, y = 0, z = 0;
            return new Vec3<T>((T)x, (T)y, (T)z);
        }
    }

    public static Vec3<T> Unit
    {
        get
        {
            dynamic x = 1, y = 1, z = 1;
            return new Vec3<T>((T)x, (T)y, (T)z);
        }
    }

    public Vec3<T> Normalize()
    {
        var max = X < Y ? X : Y;
        max = max < Z ? Z : max;
        dynamic c = 0;
        if (max > c) return new Vec3<T>(X, Y, Z) / max;
        return new Vec3<T>(X, Y, Z);
    }
    
    public static Vec3<T> operator -(Vec3<T> value)
    {
        return new Vec3<T>(-value.X, -value.Y, -value.Z);
    }

    public static implicit operator Vec3<T>(T data)
    {
        return new Vec3<T>(data);
    }

    public void Deconstruct(out T x, out T y, out T z)
    {
        x = X;
        y = Y;
        z = Z;
    }
}