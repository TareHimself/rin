namespace Rin.Core.Graphics.Shaders;

/// <summary>
/// Marks a <c>partial</c> <see cref="IComputeShader"/> property as generator-backed. The generator emits
/// <c>=&gt; field ??= IGraphicsModule.Get().MakeCompute(path)</c> for the property.
/// </summary>
/// <example>
/// <code>
/// // ReSharper disable once MemberCanBeMadeStatic.Global
/// [ComputeShader("cs/assets/test/blur.slang")]
/// public partial IComputeShader BlurShader { get; }
/// </code>
/// Or, referencing a shader-authoring class directly, which passes its generated descriptor instead:
/// <code>
/// [ComputeShader&lt;BlurShader&gt;]
/// public partial IComputeShader BlurShader { get; }
/// </code>
/// </example>
public sealed class ComputeShaderAttribute : ShaderAttribute
{
    public ComputeShaderAttribute(string path) : base(path)
    {
    }

}

/// <inheritdoc cref="ComputeShaderAttribute"/>
public sealed class ComputeShaderAttribute<T> : ShaderAttribute where T : global::Rin.Shade.Shader;
