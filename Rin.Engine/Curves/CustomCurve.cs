namespace Rin.Engine.Curves;

public abstract class CustomCurve<TValue> : Curve<CustomCurvePoint<TValue>,TValue> where TValue : struct
{
    protected override TValue Interpolate(in CustomCurvePoint<TValue> previous, in CustomCurvePoint<TValue> next, float alpha)
    {
        // Need to figure out the math here
        return InterpolateValue(previous.Value, next.Value, alpha);
    }
    
    /// <summary>
    /// Interpolate the value using the remapped alpha
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="nextValue"></param>
    /// <param name="alpha"></param>
    /// <returns></returns>
    protected abstract TValue InterpolateValue(in TValue previousValue, in TValue nextValue, float alpha);

    public void Add(float time, in TValue value, InterpMethod inMethod = InterpMethod.Linear,InterpMethod outMethod = InterpMethod.Linear)
    {
        Add(time,new CustomCurvePoint<TValue>()
        {
            Value = value,
            InMethod = inMethod,
            OutMethod = outMethod
        });
    }
}