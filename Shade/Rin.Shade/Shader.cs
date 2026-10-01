namespace Rin.Shade;

/// <summary>
/// Base class for C# shaders that the Rin.Shade transpiler lowers to Slang.
/// </summary>
public abstract partial class Shader
{
    /// <summary>
    /// Blend state of the shader's color output.
    /// </summary>
    protected virtual BlendState BlendState => BlendState.None;
}
