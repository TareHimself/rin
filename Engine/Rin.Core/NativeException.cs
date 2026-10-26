namespace Rin.Core;

/// <summary>
///     A native export returned an error code instead of <c>Ok</c>.
/// </summary>
public sealed class NativeException(string library, string function, Enum code)
    : Exception($"{function} in {library} failed with {code}")
{
    public string Library { get; } = library;
    public string Function { get; } = function;
    public Enum Code { get; } = code;
}
