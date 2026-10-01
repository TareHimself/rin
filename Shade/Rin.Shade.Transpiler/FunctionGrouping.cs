using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Reorders the functions of a valid call order so the members of one struct's extension sit together and
/// are not split up by unrelated free functions. After each function the next one is the earliest
/// still-unplaced function with the same owner whose callees are already placed, so a function is only
/// hoisted as far up as its dependencies allow.
/// </summary>
internal static class FunctionGrouping
{
    public static List<IMethodSymbol> Group(IReadOnlyList<IMethodSymbol> order,
        IReadOnlyDictionary<IMethodSymbol, HashSet<IMethodSymbol>> dependencies)
    {
        var remaining = order.ToList();
        var placed = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var result = new List<IMethodSymbol>();
        INamedTypeSymbol? currentOwner = null;

        bool IsReady(IMethodSymbol method) =>
            !dependencies.TryGetValue(method, out var callees) ||
            callees.All(callee => placed.Contains(callee) || !order.Contains(callee, SymbolEqualityComparer.Default));

        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(method =>
                           IsReady(method) && SymbolEqualityComparer.Default.Equals(Owner(method), currentOwner))
                       ?? remaining.FirstOrDefault(IsReady)
                       ?? remaining[0];

            remaining.Remove(next);
            placed.Add(next);
            result.Add(next);
            currentOwner = Owner(next);
        }

        return result;
    }

    /// <summary>The struct whose extension the function is a member of, or null for a free function.</summary>
    private static INamedTypeSymbol? Owner(IMethodSymbol method) =>
        !method.IsStatic && method.ContainingType.TypeKind == TypeKind.Struct ? method.ContainingType : null;
}
