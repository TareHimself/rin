namespace Rin.Shade;

public abstract partial class Shader
{
    [SlangStatement("discard;")]
    public static extern void Discard();

    [SlangStatement("InterlockedAdd(@0, @1, @2);")]
    public static extern void InterlockedAdd(ref uint destination, uint value, out uint original);
}
