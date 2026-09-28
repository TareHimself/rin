using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

// A shader has no runtime polymorphism - each [Shader] class transpiles to its own independent,
// monomorphic output, so "the most-derived override" is always statically knowable. But a call to
// a virtual/abstract method through implicit `this`, made from code physically declared in a
// shared base class, still resolves in Roslyn's own operation tree to whatever's lexically in
// scope at that call site - the base's own (possibly bodyless) declaration, not the override that
// will actually run. This maps every declaration along an override chain in `chain` to its real
// most-derived implementation, so a call site can be resolved to the body that's actually reachable.
internal static class OverrideResolution
{
    public static Dictionary<IMethodSymbol, IMethodSymbol> Build(List<INamedTypeSymbol> chain)
    {
        var allMethods = chain.SelectMany(t => t.GetMembers().OfType<IMethodSymbol>()).ToList();
        var shadowed = new HashSet<IMethodSymbol>(
            allMethods.Select(m => m.OverriddenMethod).Where(m => m is not null)!,
            SymbolEqualityComparer.Default);

        var map = new Dictionary<IMethodSymbol, IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var effective in allMethods.Where(m => !shadowed.Contains(m)))
        for (var current = effective; current is not null; current = current.OverriddenMethod)
            map[current] = effective;

        return map;
    }

    public static IMethodSymbol Resolve(IMethodSymbol method, IReadOnlyDictionary<IMethodSymbol, IMethodSymbol> map) =>
        map.TryGetValue(method, out var effective) ? effective : method;
}
