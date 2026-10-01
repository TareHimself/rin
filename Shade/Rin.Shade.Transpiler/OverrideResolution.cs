using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

// A shader has no runtime polymorphism, so the most-derived override is always statically
// knowable - but a call made from code declared in a base class still resolves, in Roslyn's own
// operation tree, to the base's own (possibly bodyless) declaration. This maps every declaration
// along an override chain to its real most-derived implementation.
internal static class OverrideResolution
{
    public static IEnumerable<IMethodSymbol> EffectiveMethods(List<INamedTypeSymbol> chain)
    {
        var allMethods = chain.SelectMany(t => t.GetMembers().OfType<IMethodSymbol>()).ToList();
        var shadowed = new HashSet<IMethodSymbol>(
            allMethods.Select(m => m.OverriddenMethod).Where(m => m is not null)!,
            SymbolEqualityComparer.Default);
        return allMethods.Where(m => !shadowed.Contains(m));
    }

    public static Dictionary<IMethodSymbol, IMethodSymbol> Build(List<INamedTypeSymbol> chain)
    {
        var map = new Dictionary<IMethodSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var effective in EffectiveMethods(chain))
        for (var current = effective; current is not null; current = current.OverriddenMethod)
        {
            map[current] = effective;

            // A body declared in an open generic base references OriginalDefinition (TData), not
            // the substituted form above (ConstructedFrom doesn't unwrap containing-type generics).
            if (!SymbolEqualityComparer.Default.Equals(current, current.OriginalDefinition))
                map[current.OriginalDefinition] = effective;
        }

        return map;
    }

    public static IMethodSymbol Resolve(IMethodSymbol method, IReadOnlyDictionary<IMethodSymbol, IMethodSymbol> map) =>
        map.TryGetValue(method, out var effective) ? effective : method;
}
