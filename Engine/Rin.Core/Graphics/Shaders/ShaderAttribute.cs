namespace Rin.Core.Graphics.Shaders;

/// <summary>
/// Common base for <see cref="GraphicsShaderAttribute"/> and <see cref="ComputeShaderAttribute"/>. Marks a
/// <c>partial</c> property as generator-backed: the source generator emits the other half of the partial
/// property, returning the shader loaded from <see cref="Path"/> via
/// <see cref="global::Rin.Core.Graphics.IGraphicsModule.Get"/>.
/// </summary>
/// <remarks>
/// The property must be an instance member (the generator rejects <c>static</c> properties). Rider/ReSharper
/// only inspects the hand-written defining declaration - it excludes the generator's own output from analysis
/// because that file is marked <c>&lt;auto-generated/&gt;</c> - so it can't see the <c>field ??= ...</c> body
/// that actually uses instance state, and may incorrectly suggest "member can be made static". Suppress that
/// with <c>// ReSharper disable once MemberCanBeMadeStatic.Global</c> (or <c>.Local</c> for a non-public
/// property) above the declaration, as shown in the examples on <see cref="GraphicsShaderAttribute"/> and
/// <see cref="ComputeShaderAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public abstract class ShaderAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}
