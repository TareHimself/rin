using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Math;

namespace Rin.Engine;

public struct Bounds3D : IAdditionOperators<Bounds3D, Bounds3D, Bounds3D>
{
    public Bounds1D X;
    public Bounds1D Y;
    public Bounds1D Z;

    /// <summary>
    ///     Combines two bounds together
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Bounds3D operator +(Bounds3D left, Bounds3D right)
    {
        return new Bounds3D
        {
            X = left.X + right.X,
            Y = left.Y + right.Y,
            Z = left.Z + right.Z
        };
    }

    public static Bounds3D FromVector(in Vector3 location)
    {
        return new Bounds3D
        {
            X = new Bounds1D
            {
                Min = location.X,
                Max = location.X
            },
            Y = new Bounds1D
            {
                Min = location.Y,
                Max = location.Y
            },
            Z = new Bounds1D
            {
                Min = location.Z,
                Max = location.Z
            }
        };
    }

    [Pure]
    public Bounds3D Transform(in Matrix4x4 matrix)
    {
        var p1 = new Vector4(X.Min, Y.Min, Z.Min, 1f).Transform(matrix);
        var p2 = new Vector4(X.Min, Y.Min, Z.Max, 1f).Transform(matrix);
        var p3 = new Vector4(X.Max, Y.Min, Z.Min, 1f).Transform(matrix);
        var p4 = new Vector4(X.Max, Y.Min, Z.Max, 1f).Transform(matrix);
        var p5 = new Vector4(X.Min, Y.Max, Z.Min, 1f).Transform(matrix);
        var p6 = new Vector4(X.Min, Y.Max, Z.Max, 1f).Transform(matrix);
        var p7 = new Vector4(X.Max, Y.Max, Z.Min, 1f).Transform(matrix);
        var p8 = new Vector4(X.Max, Y.Max, Z.Max, 1f).Transform(matrix);

        var min = Vector4.Min(p1, p2);
        min = Vector4.Min(min, p3);
        min = Vector4.Min(min, p4);
        min = Vector4.Min(min, p5);
        min = Vector4.Min(min, p6);
        min = Vector4.Min(min, p7);
        min = Vector4.Min(min, p8);

        var max = Vector4.Max(p1, p2);
        max = Vector4.Max(max, p3);
        max = Vector4.Max(max, p4);
        max = Vector4.Max(max, p5);
        max = Vector4.Max(max, p6);
        max = Vector4.Max(max, p7);
        max = Vector4.Max(max, p8);

        return FromVector(new Vector3(min.X, min.Y, min.Z)) + FromVector(new Vector3(max.X, max.Y, max.Z));
    }
}