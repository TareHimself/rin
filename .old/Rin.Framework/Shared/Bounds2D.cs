using System.Numerics;

namespace Rin.Framework.Shared;

public record struct Bounds2D : IAdditionOperators<Bounds2D, Bounds2D, Bounds2D>
{
    public Bounds1D X;
    public Bounds1D Y;

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
            X = left.X + right.X,
            Y = left.Y + right.Y
        };
    }
}