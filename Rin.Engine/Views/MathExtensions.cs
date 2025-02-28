using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views;

public static class MathExtensions
{
    public static Vector2 ApplyTransformation(this Vector2 src, Mat3 matrix)
    {
        var vector3 = new Vector3(src.X, src.Y, 1.0f);
        var res = matrix * vector3;
        return new Vector2(res.X, res.Y);
    }
}