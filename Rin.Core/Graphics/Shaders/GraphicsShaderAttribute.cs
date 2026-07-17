namespace Rin.Core.Graphics.Shaders;

/// <summary>
/// Marks a <c>partial</c> <see cref="IGraphicsShader"/> property as generator-backed. The generator emits
/// <c>=&gt; field ??= IGraphicsModule.Get().MakeGraphics(path)</c> for the property.
/// </summary>
/// <example>
/// <code>
/// // ReSharper disable once MemberCanBeMadeStatic.Global
/// [GraphicsShader("fs/assets/test/pretty.slang")]
/// public partial IGraphicsShader PrettyShader { get; }
/// </code>
/// </example>
public sealed class GraphicsShaderAttribute(string path) : ShaderAttribute(path);
