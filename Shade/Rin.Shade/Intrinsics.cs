namespace Rin.Shade;

/// <summary>
/// Low-level shader intrinsics.
/// </summary>
public static class Intrinsics
{
    /// <summary>
    /// Reinterprets the bits of a value as <typeparamref name="T" /> (Slang <c>reinterpret</c>).
    /// </summary>
    [SlangExpression("reinterpret<@T0>(@0)")]
    public static extern T Reinterpret<T>(this object value);
}
