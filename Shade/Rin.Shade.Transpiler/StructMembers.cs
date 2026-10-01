using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// An instance data member of a struct: its name, type and the field that stores it.
/// </summary>
internal readonly record struct StructMember(string Name, ITypeSymbol Type, IFieldSymbol Field);

/// <summary>
/// Enumerates the data members of a struct.
/// </summary>
internal static class StructMembers
{
    /// <summary>
    /// A struct's instance data members - declared fields plus the backing field of each auto
    /// property, named after the property (the backing field itself has a compiler-generated name).
    /// </summary>
    public static IEnumerable<StructMember> Instance(INamedTypeSymbol type)
    {
        foreach (var field in type.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic))
        {
            if (!field.IsImplicitlyDeclared)
                yield return new StructMember(field.Name, field.Type, field);
            else if (field.AssociatedSymbol is IPropertySymbol property)
                yield return new StructMember(property.Name, field.Type, field);
        }
    }

    /// <summary>
    /// Whether the property is backed by a compiler-generated field.
    /// </summary>
    public static bool IsAutoProperty(IPropertySymbol property) =>
        property.ContainingType.GetMembers().OfType<IFieldSymbol>()
            .Any(f => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, property));
}
