namespace Rin.Core.Graphics.Shaders;

/// <summary>
/// Marks a <c>partial</c> <see cref="IComputeShader"/> property as generator-backed. The generator emits
/// <c>=&gt; field ??= IGraphicsModule.Get().MakeCompute(T.Descriptor)</c> for the property.
/// </summary>
/// <example>
/// <code>
/// // ReSharper disable once MemberCanBeMadeStatic.Global
/// [ComputeShader&lt;BlurShader&gt;]
/// public partial IComputeShader BlurShader { get; }
/// </code>
/// </example>
public sealed class ComputeShaderAttribute<T> : ShaderAttribute where T : global::Rin.Shade.Shader;
