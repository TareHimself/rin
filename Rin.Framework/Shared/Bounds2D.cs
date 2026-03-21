using System.Numerics;

namespace Rin.Framework.Shared;

public record struct Bounds2D : IAdditionOperators<Bounds2D, Bounds2D, Bounds2D>
{
    public Vector2 Min;
    public Vector2 Max;

    /// <summary>
    ///     Combines two bounds together
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Bounds2D operator +(Bounds2D left, Bounds2D right)
    {
        return new Bounds2D
        {
            Min = Vector2.Min(left.Min, right.Min),
            Max = Vector2.Max(left.Max, right.Max)
        };
    }
}