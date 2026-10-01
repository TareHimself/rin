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
/// Or, referencing a shader-authoring class directly, which passes its generated descriptor instead:
/// <code>
/// [GraphicsShader&lt;PrettyShader&gt;]
/// public partial IGraphicsShader PrettyShader { get; }
/// </code>
/// </example>
public sealed class GraphicsShaderAttribute : ShaderAttribute
{
    public GraphicsShaderAttribute(string path) : base(path)
    {
    }

}

/// <inheritdoc cref="GraphicsShaderAttribute"/>
public sealed class GraphicsShaderAttribute<T> : ShaderAttribute where T : global::Rin.Shade.Shader;
