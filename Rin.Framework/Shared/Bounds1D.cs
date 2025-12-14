using System.Numerics;

namespace Rin.Framework.Shared;

public record struct Bounds1D : IAdditionOperators<Bounds1D, Bounds1D, Bounds1D>
{
    public float Max;
    public float Min;

    /// <summary>
    ///     Combines two bounds together
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Bounds1D operator +(Bounds1D left, Bounds1D right)
    {
        return new Bounds1D
        {
            Min = float.Min(left.Min, right.Min),
            Max = float.Max(left.Max, right.Max)
        };
    }
}