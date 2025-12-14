namespace Rin.Framework.Curves;

public class FloatCurve : AdvancedCurve<float>
{
    protected override float LinearInterpolateValue(in float previous, in float next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }
}