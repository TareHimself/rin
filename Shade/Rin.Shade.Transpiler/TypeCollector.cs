using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Walks a type's fields to find every plain struct or enum reachable from it (post-order, so a
/// dependency is always collected before the type that contains it - Slang has no forward
/// declarations for structs or enums).
/// </summary>
internal static class TypeCollector
{
    public static void Collect(ITypeSymbol type, List<INamedTypeSymbol> order, HashSet<INamedTypeSymbol> visited)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            Collect(arrayType.ElementType, order, visited);
            return;
        }

        if (type is not INamedTypeSymbol named) return;

        if (TypeMapping.IsBuiltIn(named))
        {
            if (named is { Name: "BufferRef", TypeArguments.Length: 1 })
                Collect(named.TypeArguments[0], order, visited);
            return;
        }

        if (named.TypeKind is not (TypeKind.Struct or TypeKind.Enum)) return;
        if (!visited.Add(named)) return;

        if (named.TypeKind == TypeKind.Struct)
        {
            foreach (var field in named.GetMembers().OfType<IFieldSymbol>()
                         .Where(f => !f.IsStatic && !f.IsImplicitlyDeclared))
                Collect(field.Type, order, visited);
        }

        order.Add(named);
    }
}
