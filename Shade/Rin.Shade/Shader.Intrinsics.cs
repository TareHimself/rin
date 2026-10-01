using System.Numerics;

namespace Rin.Shade;

public abstract partial class Shader
{
    /// <summary>
    /// Discards the current fragment.
    /// </summary>
    [SlangStatement("discard;")]
    public static extern void Discard();

    /// <summary>
    /// Atomically adds <paramref name="value" /> to <paramref name="destination" />, returning the prior value in <paramref name="original" />.
    /// </summary>
    [SlangStatement("InterlockedAdd(@0, @1, @2);")]
    public static extern void InterlockedAdd(ref uint destination, uint value, out uint original);

    /// <summary>
    /// Sum of the absolute screen-space derivatives of <paramref name="value" />.
    /// </summary>
    [SlangExpression("fwidth(@0)")]
    public static extern float Fwidth(float value);

    [SlangExpression("fwidth(@0)")]
    public static extern Vector2 Fwidth(Vector2 value);

    /// <summary>
    /// Reinterprets the bits of a float as a uint.
    /// </summary>
    [SlangExpression("asuint(@0)")]
    public static extern uint AsUInt(float value);

    /// <summary>
    /// Marks a descriptor index as non-uniform across the wave.
    /// </summary>
    [SlangExpression("NonUniformResourceIndex(@0)")]
    public static extern uint NonUniformResourceIndex(uint index);
}
