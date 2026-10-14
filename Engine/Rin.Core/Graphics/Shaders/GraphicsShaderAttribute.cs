namespace Rin.Core.Graphics.Shaders;

/// <summary>
/// Marks a <c>partial</c> <see cref="IGraphicsShader"/> property as generator-backed. The generator emits
/// <c>=&gt; field ??= IGraphicsModule.Get().MakeGraphics(T.Descriptor)</c> for the property.
/// </summary>
/// <example>
/// <code>
/// // ReSharper disable once MemberCanBeMadeStatic.Global
/// [GraphicsShader&lt;PrettyShader&gt;]
/// public partial IGraphicsShader PrettyShader { get; }
/// </code>
/// </example>
public sealed class GraphicsShaderAttribute<T> : ShaderAttribute where T : global::Rin.Shade.Shader;
