using System.Numerics;

namespace rin.Framework.Core.Math;

public static class MathExtensions
{
    
    /// <summary>
    /// If <see cref="val"/> is finite returns <see cref="val"/> else <see cref="other"/> which is zero by default
    /// </summary>
    /// <param name="val"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static float FiniteOr(this float val, float other = 0.0f) => float.IsFinite(val) ? val : other;
    
    public static Vector2<float> FiniteOr(this Vector2<float> val, float x = 0.0f,float y = 0.0f) => new (float.IsFinite(val.X) ? val.X : x,float.IsFinite(val.Y) ? val.Y : y);

    public static Vector2<float> Abs(this Vector2<float> self) => new (System.Math.Abs(self.X), System.Math.Abs(self.Y));
    
    public static bool NearlyEquals(this Vector2<float> self, Vector2<float> other,float tolerance = 0.01f)
    {
        return (self - other).Abs() < tolerance;
    }
    
    public static double Distance(this Vector2<float> a, Vector2<float> b)
    {
        return System.Math.Sqrt(System.Math.Pow((b.X - a.X),2) + System.Math.Pow((b.Y - a.Y),2));
    }
}