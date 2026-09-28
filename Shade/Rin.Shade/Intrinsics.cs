namespace Rin.Shade;

/// <summary>
/// Language-level intrinsics with no C# body to walk - hardware/Slang facts, bound the same way
/// any other [SlangCall]/[SlangStatement] stub is, not privileged.
/// </summary>
public static class Intrinsics
{
    [SlangStatement("discard;")]
    public static extern void Discard();

    [SlangCall("reinterpret<$T0>($0)")]
    public static extern T Reinterpret<T>(this object value);
}
