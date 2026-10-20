using System.Collections.Generic;
using System.Linq;
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
            containers.Insert(0, TypeName(container));
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
    public static string ToSlangMethodName(IMethodSymbol method)
    {
        var name = method.MethodKind switch
        {
            MethodKind.PropertyGet => ToSlangIdentifier($"Get{method.AssociatedSymbol!.Name}"),
            MethodKind.PropertySet => ToSlangIdentifier($"Set{method.AssociatedSymbol!.Name}"),
            _ => ToSlangIdentifier(method.Name)
        };

        // One Slang function per instantiation, told apart by their type arguments: Pick<float> and
        // Pick<float3> become pick_float and pick_float3.
        return method.IsGenericMethod
            ? name + string.Concat(method.TypeArguments.Select(argument => "_" + TypeTag(argument)))
            : name;
    }

    /// <summary>
    /// A type's spelling inside an identifier, independent of the name scope so a declaration and its call
    /// sites agree: builtins by their Slang name, a BufferRef by its element plus Ptr, anything else by its name.
    /// </summary>
    public static string TypeTag(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { Name: "BufferRef", TypeArguments.Length: 1 } buffer)
            return TypeTag(buffer.TypeArguments[0]) + "Ptr";
        if (type is INamedTypeSymbol { IsGenericType: true } generic) return TypeName(generic);
        return TypeMapping.IsBuiltIn(type) ? TypeMapping.MapType(type) : type.Name;
    }

    /// <summary>
    /// The unqualified Slang name of a declared type. A generic struct is lowered once per instantiation, told
    /// apart by its type arguments: Wrapper&lt;float&gt; and Wrapper&lt;float3&gt; become Wrapper_float and Wrapper_float3.
    /// </summary>
    public static string TypeName(INamedTypeSymbol type) =>
        type.IsGenericType
            ? type.Name + string.Concat(type.TypeArguments.Select(argument => "_" + TypeTag(argument)))
            : type.Name;

    /// <summary>
    /// Whether the type still mentions a type parameter anywhere in its arguments or containers, so it has no
    /// concrete Slang declaration.
    /// </summary>
    public static bool IsOpenGeneric(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        INamedTypeSymbol named => named.TypeArguments.Any(IsOpenGeneric) ||
                                  named.ContainingType is { } container && IsOpenGeneric(container),
        IArrayTypeSymbol array => IsOpenGeneric(array.ElementType),
        _ => false
    };
}
