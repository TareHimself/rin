namespace Rin.Slang.Compiler;

/// <summary>
///     Thrown when a `.slang` file has no `compute`/`vertex`/`fragment` entry point — i.e. it's an
///     include-only library file, not a compilable shader. Distinct from other <see cref="SlangCompileException" />
///     failures so bulk callers (<see cref="ShaderCompiler.TryCompile" />) can treat it as "skip", not "error".
/// </summary>
public sealed class NotAShaderException(string message) : SlangCompileException(message);
