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
/// </example>
public sealed class ComputeShaderAttribute(string path) : ShaderAttribute(path);
