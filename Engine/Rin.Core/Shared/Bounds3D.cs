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

    /// <summary>
    ///     The axis-aligned box that encloses this one after the transform, rotation included
    /// </summary>
    [Pure]
    public Bounds3D Transform(in Matrix4x4 matrix)
    {
        var center = ((Min + Max) * 0.5f).Transform(matrix);
        var extent = (Max - Min) * 0.5f;
        var transformedExtent = new Vector3(
            extent.X * MathF.Abs(matrix.M11) + extent.Y * MathF.Abs(matrix.M21) + extent.Z * MathF.Abs(matrix.M31),
            extent.X * MathF.Abs(matrix.M12) + extent.Y * MathF.Abs(matrix.M22) + extent.Z * MathF.Abs(matrix.M32),
            extent.X * MathF.Abs(matrix.M13) + extent.Y * MathF.Abs(matrix.M23) + extent.Z * MathF.Abs(matrix.M33));

        return new Bounds3D
        {
            Min = center - transformedExtent,
            Max = center + transformedExtent
        };
    }

    public void Update(in Vector3 location)
    {
        Min = Vector3.Min(Min, location);
        Max = Vector3.Max(Max, location);
    }
}