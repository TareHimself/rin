namespace Rin.Shade;

public static class Intrinsics
{
    [SlangExpression("reinterpret<@T0>(@0)")]
    public static extern T Reinterpret<T>(this object value);
}
