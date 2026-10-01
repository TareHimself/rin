using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Maps C# names and namespaces to their Slang spellings.
/// </summary>
internal static class Naming
{
    /// <summary>
    /// C# members are PascalCase, Slang members are camelCase - every emitted identifier goes
    /// through this so the two languages agree without hand-maintained per-field renames.
    /// </summary>
    public static string ToSlangIdentifier(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    /// <summary>
    /// A type's C# namespace as a Slang namespace path (Rin.Core.Views becomes Rin::Core::Views), or
    /// empty for the global namespace. `::` rather than `.` so a namespace path can never be read as
    /// a member access chain.
    /// </summary>
    public static string NamespacePath(ISymbol type)
    {
        var segments = new List<string>();
        if (type.ContainingNamespace is { IsGlobalNamespace: false } ns)
            segments.AddRange(ns.ToDisplayString().Split('.'));

        // Containing types are part of the path: a struct nested in a struct is declared inside it
        // (Rin::Views::Quad::LineData), and one nested in a class reads as a namespace named after the class.
        var containers = new List<string>();
        for (var container = type.ContainingType; container is not null; container = container.ContainingType)
            containers.Insert(0, container.Name);
        segments.AddRange(containers);

        return string.Join("::", segments);
    }

    /// <summary>
    /// Marks a struct Rin.Shade invents (a union's per-variant and complete structs) rather than one
    /// the C# source declares, so its name can never collide with a user's type - and so generated
    /// code is easy to spot when reading the Slang.
    /// </summary>
    public const string GeneratedPrefix = "__Shade__";

    /// <summary>
    /// The Slang name of a type declared (or generated) under its namespace, relative to the scope being written.
    /// </summary>
    public static string Qualify(ISymbol type, string name) => NameScope.Name(NamespacePath(type), name);

    /// <summary>
    /// Like <see cref="ToSlangIdentifier"/>, but a property accessor becomes "getFoo"/"setFoo" instead of
    /// its compiler-generated "get_Foo"/"set_Foo". Declaration and call sites both use this so they agree.
    /// </summary>
    public static string ToSlangMethodName(IMethodSymbol method) => method.MethodKind switch
    {
        MethodKind.PropertyGet => ToSlangIdentifier($"Get{method.AssociatedSymbol!.Name}"),
        MethodKind.PropertySet => ToSlangIdentifier($"Set{method.AssociatedSymbol!.Name}"),
        _ => ToSlangIdentifier(method.Name)
    };
}
