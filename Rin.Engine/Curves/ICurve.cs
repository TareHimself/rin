namespace Rin.Engine.Curves;

public interface ICurve<out TInterpolatedValue> where TInterpolatedValue : struct
{
    public TInterpolatedValue Sample(float time);
}