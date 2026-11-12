using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Shared.Math;

namespace Rin.Core.Shared;

[NoReorder]
public record struct Bounds3D : IAdditionOperators<Bounds3D, Bounds3D, Bounds3D>
{
    public Vector3 Min;
    public Vector3 Max;


    /// <summary>
    ///     Combines two bounds together
    /// </summary>
    public static Bounds3D operator +(Bounds3D left, Bounds3D right)
    {
        return new Bounds3D
        {
            Min = Vector3.Min(left.Min, right.Min),
            Max = Vector3.Max(left.Max, right.Max)
        };
    }

    [Pure]
    public Bounds3D Expand(float amount)
    {
        return new Bounds3D
        {
            Min = Min - new Vector3(amount),
            Max = Max + new Vector3(amount)
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