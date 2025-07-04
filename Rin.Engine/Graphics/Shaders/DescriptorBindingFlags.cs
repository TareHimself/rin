namespace Rin.Engine.Graphics.Shaders;

[Flags]
public enum DescriptorBindingFlags
{
    None = 0,
    UpdateAfterBind = 1 << 0,
    PartiallyBound = 1 << 1,
    Variable = 1 << 2,
}