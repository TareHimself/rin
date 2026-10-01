using System.Numerics;

namespace Rin.Shade;

public abstract partial class Shader
{
    [SlangStatement("discard;")]
    public static extern void Discard();

    [SlangStatement("InterlockedAdd(@0, @1, @2);")]
    public static extern void InterlockedAdd(ref uint destination, uint value, out uint original);

    [SlangExpression("fwidth(@0)")]
    public static extern float Fwidth(float value);

    [SlangExpression("fwidth(@0)")]
    public static extern Vector2 Fwidth(Vector2 value);

    [SlangExpression("asuint(@0)")]
    public static extern uint AsUInt(float value);

    [SlangExpression("NonUniformResourceIndex(@0)")]
    public static extern uint NonUniformResourceIndex(uint index);
}
