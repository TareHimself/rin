namespace Rin.Framework.Curves;

public struct CustomCurvePoint<T> where T : struct
{
    public T Value;
    public SampleMethod Method;
    public float Tangent;
}