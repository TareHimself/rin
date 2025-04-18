﻿using System.Numerics;

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

    public static Bounds3D FromLocation(Vector3 location)
    {
        return new Bounds3D
        {
            X = new Bounds1D()
            {
                Min = location.X,
                Max = location.X,
            },
            Y = new Bounds1D()
            {
                Min = location.Y,
                Max = location.Y,
            },
            Z = new Bounds1D()
            {
                Min = location.Z,
                Max = location.Z,
            },
        };
    }
}