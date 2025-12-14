using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Shared.Math;

namespace Rin.Framework.Shared;

public record struct Bounds3D : IAdditionOperators<Bounds3D, Bounds3D, Bounds3D>
{
    public Vector3 Max;
    public Vector3 Min;

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
            Min = Vector3.Min(left.Min, right.Min),
            Max = Vector3.Max(left.Max, right.Max)
        };
    }

    public static Bounds3D FromVector(in Vector3 location)
    {
        return new Bounds3D
        {
            Min = location,
            Max = location
        };
    }

    [Pure]
    public Bounds3D Transform(in Matrix4x4 matrix)
    {
        var min = Min.Transform(matrix);
        var max = Max.Transform(matrix);

        var temp = min;

        min = Vector3.Min(min, max);
        max = Vector3.Max(max, temp);

        return new Bounds3D
        {
            Min = min,
            Max = max
        };
    }

    public void Update(in Vector3 location)
    {
        Min = Vector3.Min(Min, location);
        Max = Vector3.Max(Max, location);
    }
}