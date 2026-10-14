using System.Numerics;
using Rin.Shade;

namespace Rin.Core.Views;

/// <summary>
///     The corner radii of a rounded rectangle, in CSS order. Four floats in sequence, so the same struct is
///     what the quad data carries and what the shader reads.
/// </summary>
[ShadeExport]
public struct BorderRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
    : IEquatable<BorderRadius>
{
    public float TopLeft = topLeft;
    public float TopRight = topRight;
    public float BottomRight = bottomRight;
    public float BottomLeft = bottomLeft;

    public static BorderRadius Zero => default;

    /// <summary>
    ///     Written with field assignments rather than the constructor so a shader can call it too.
    /// </summary>
    public static BorderRadius All(float radius)
    {
        BorderRadius result;
        result.TopLeft = radius;
        result.TopRight = radius;
        result.BottomRight = radius;
        result.BottomLeft = radius;
        return result;
    }

    public static BorderRadius Top(float radius)
    {
        return new BorderRadius(radius, radius, 0f, 0f);
    }

    public static BorderRadius Bottom(float radius)
    {
        return new BorderRadius(0f, 0f, radius, radius);
    }

    public static BorderRadius Left(float radius)
    {
        return new BorderRadius(radius, 0f, 0f, radius);
    }

    public static BorderRadius Right(float radius)
    {
        return new BorderRadius(0f, radius, radius, 0f);
    }

    public static implicit operator BorderRadius(float radius)
    {
        return All(radius);
    }

    public static BorderRadius operator +(BorderRadius radius, float amount)
    {
        return new BorderRadius(radius.TopLeft + amount, radius.TopRight + amount, radius.BottomRight + amount,
            radius.BottomLeft + amount);
    }

    public static BorderRadius operator -(BorderRadius radius, float amount)
    {
        return radius + -amount;
    }

    public static BorderRadius operator *(BorderRadius radius, float factor)
    {
        return new BorderRadius(radius.TopLeft * factor, radius.TopRight * factor, radius.BottomRight * factor,
            radius.BottomLeft * factor);
    }

    public static bool operator ==(BorderRadius left, BorderRadius right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BorderRadius left, BorderRadius right)
    {
        return !left.Equals(right);
    }

    public Vector4 ToVector4()
    {
        return new Vector4(TopLeft, TopRight, BottomRight, BottomLeft);
    }

    public bool Equals(BorderRadius other)
    {
        return TopLeft.Equals(other.TopLeft) && TopRight.Equals(other.TopRight) &&
               BottomRight.Equals(other.BottomRight) && BottomLeft.Equals(other.BottomLeft);
    }

    public override bool Equals(object? obj)
    {
        return obj is BorderRadius other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TopLeft, TopRight, BottomRight, BottomLeft);
    }

    public override string ToString()
    {
        return $"({TopLeft}, {TopRight}, {BottomRight}, {BottomLeft})";
    }
}
