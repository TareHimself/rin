namespace Rin.Engine.Core.Curves;

public class FloatCurve : Curve<float>
{
    protected override float Interpolate(float alpha, float previous, float next)
    {
        var dist = next - previous;
        return previous + (dist * alpha);
    }
}