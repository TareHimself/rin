namespace Rin.Engine.Curves;

public struct CustomCurvePoint<T> where T : struct
{
    public T Value;
    public SampleMethod Method;
    public float Tangent;
}