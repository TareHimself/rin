using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Maps every declaration along an override chain to its most-derived implementation. A shader has no
/// runtime polymorphism, but a call written in a base class still binds to the base declaration, which
/// may have no body.
/// </summary>
internal static class OverrideResolution
{
    /// <summary>
    /// The methods of the type chain that no other method in the chain overrides.
    /// </summary>
    public static IEnumerable<IMethodSymbol> EffectiveMethods(List<INamedTypeSymbol> chain)
    {
        var allMethods = chain.SelectMany(t => t.GetMembers().OfType<IMethodSymbol>()).ToList();
        var shadowed = new HashSet<IMethodSymbol>(
            allMethods.Select(m => m.OverriddenMethod).Where(m => m is not null)!,
            SymbolEqualityComparer.Default);
        return allMethods.Where(m => !shadowed.Contains(m));
    }

    /// <summary>
    /// A map from each overridden declaration to the method that finally overrides it.
    /// </summary>
    public static Dictionary<IMethodSymbol, IMethodSymbol> Build(List<INamedTypeSymbol> chain)
    {
        var map = new Dictionary<IMethodSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var effective in EffectiveMethods(chain))
        for (var current = effective; current is not null; current = current.OverriddenMethod)
        {
            map[current] = effective;

            // A body in an open generic base references OriginalDefinition, not the substituted form.
            if (!SymbolEqualityComparer.Default.Equals(current, current.OriginalDefinition))
                map[current.OriginalDefinition] = effective;
        }

        return map;
    }

    /// <summary>
    /// The effective implementation of the method, or the method itself if it is not overridden.
    /// </summary>
    public static IMethodSymbol Resolve(IMethodSymbol method, IReadOnlyDictionary<IMethodSymbol, IMethodSymbol> map) =>
        map.TryGetValue(method, out var effective) ? effective : method;
}
