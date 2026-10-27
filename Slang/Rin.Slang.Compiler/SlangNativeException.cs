namespace Rin.Slang.Compiler;

// Mirrors SlangNativeResult in native/Rin.Slang.Native/src/result.hpp. Keep the two in sync.
public enum SlangNativeResult
{
    Ok = 0,
    InvalidArgument,
    CompileFailed,
    EntryPointNotFound,
    CompositionFailed,
    LinkFailed,
    SlangError,
    OutOfMemory,
    PlatformError
}

/// <summary>
///     The native Slang wrapper returned an error code that is not a shader problem.
/// </summary>
public sealed class SlangNativeException(string function, SlangNativeResult code)
    : Exception($"{function} in Rin.Slang.Native failed with {code}")
{
    public string Function { get; } = function;
    public SlangNativeResult Code { get; } = code;
}
