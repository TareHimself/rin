namespace Rin.Engine.Curves;

public class FloatCurve : Curve<float,float>
{
    
    
    protected override float Interpolate(in float previous, in float next, float alpha)
    {
        var dist = next - previous;
        return previous + dist * alpha;
    }

    protected override float ToInterpolatedValue(in float value)
    {
        return value;
    }
}