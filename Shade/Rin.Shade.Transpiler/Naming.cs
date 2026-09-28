namespace Rin.Shade.Transpiler;

internal static class Naming
{
    /// <summary>
    /// C# members are PascalCase, Slang members are camelCase - every emitted identifier goes
    /// through this so the two languages agree without hand-maintained per-field renames.
    /// </summary>
    public static string ToSlangIdentifier(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
