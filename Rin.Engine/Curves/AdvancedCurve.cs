namespace Rin.Engine.Curves;

/// <summary>
///     A curve class with custom interpolation methods
/// </summary>
/// <typeparam name="TValue"></typeparam>
public abstract class AdvancedCurve<TValue> : Curve<CustomCurvePoint<TValue>, TValue> where TValue : struct
{
    private float HermiteRemap(float t, float tangent)
    {
        var t2 = t * t;
        var t3 = t2 * t;

        // Hermite basis functions
        var h00 = 2 * t3 - 3 * t2 + 1;
        var h10 = t3 - 2 * t2 + t;
        var h01 = -2 * t3 + 3 * t2;
        var h11 = t3 - t2;

        // Assume value range from 0 to 1 (we're just remapping alpha)
        // Tangent affects the curve shape; p0 = 0, p1 = 1, m0 = tangent, m1 = 0
        return h00 * 0f + h10 * tangent + h01 * 1f + h11 * 0f;
    }

    protected override TValue Interpolate(in CustomCurvePoint<TValue> previous, in CustomCurvePoint<TValue> next,
        float alpha)
    {
        var remappedAlpha = previous.Method switch
        {
            SampleMethod.Step => 0f,
            SampleMethod.Linear => alpha,
            SampleMethod.Cubic => HermiteRemap(alpha, previous.Tangent),
            _ => alpha
        };
        return LinearInterpolateValue(previous.Value, next.Value, remappedAlpha);
    }

    /// <summary>
    ///     Interpolate the value using the remapped alpha
    /// </summary>
    /// <param name="previous"></param>
    /// <param name="next"></param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    protected abstract TValue LinearInterpolateValue(in TValue previous, in TValue next, float alpha);


    protected override TValue ToInterpolatedValue(in CustomCurvePoint<TValue> value)
    {
        return value.Value;
    }

    public void Add(float time, in TValue value, SampleMethod method, float tangent)
    {
        Add(time, new CustomCurvePoint<TValue>
        {
            Value = value,
            Method = method,
            Tangent = tangent
        });
    }

    public void AddLinear(float time, in TValue value)
    {
        Add(time, value, SampleMethod.Linear, 0);
    }

    public void AddStep(float time, in TValue value)
    {
        Add(time, value, SampleMethod.Step, 0);
    }

    public void AddCubic(float time, in TValue value, float tangent)
    {
        Add(time, value, SampleMethod.Cubic, tangent);
    }
}