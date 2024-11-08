namespace rin.Core.Math;

public static class MathExtensions
{
    
    /// <summary>
    /// If <see cref="val"/> is finite returns <see cref="val"/> else <see cref="other"/> which is zero by default
    /// </summary>
    /// <param name="val"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static float FiniteOr(this float val, float other = 0.0f) => float.IsFinite(val) ? val : other;
}