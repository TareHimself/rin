using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

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

        // A type's containing types are part of its path too: a struct nested in a struct is
        // declared inside it (Rin::Views::Quad::LineData), and one nested in a class - which Slang
        // has no equivalent of - reads as a namespace of the class's name.
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

    /// <summary>The fully qualified Slang name of a type declared (or generated) under its namespace.</summary>
    public static string Qualify(ISymbol type, string name)
    {
        var path = NamespacePath(type);
        return path.Length == 0 ? name : $"{path}::{name}";
    }

    /// <summary>
    /// Like <see cref="ToSlangIdentifier"/>, but for a method that may be a property accessor - its
    /// raw symbol name is the compiler-generated "get_Foo"/"set_Foo", which would emit as literally
    /// that. Lowered to "getFoo"/"setFoo" instead, both at the declaration site (FunctionLowering)
    /// and every call site (BodyLowering's property-read lowering), so both agree.
    /// </summary>
    public static string ToSlangMethodName(IMethodSymbol method) => method.MethodKind switch
    {
        MethodKind.PropertyGet => ToSlangIdentifier($"Get{method.AssociatedSymbol!.Name}"),
        MethodKind.PropertySet => ToSlangIdentifier($"Set{method.AssociatedSymbol!.Name}"),
        _ => ToSlangIdentifier(method.Name)
    };
}
