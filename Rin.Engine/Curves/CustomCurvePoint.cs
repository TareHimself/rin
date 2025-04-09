namespace Rin.Engine.Curves;

public struct CustomCurvePoint<T> where T : struct
{
    public T Value;
    public InterpMethod InMethod;
    public InterpMethod OutMethod;
}