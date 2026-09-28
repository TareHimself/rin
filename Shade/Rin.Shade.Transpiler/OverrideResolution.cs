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
    // Every method declared anywhere in the chain that isn't itself pointed to by some other
    // method's OverriddenMethod - i.e. the most-derived implementation for each override slot.
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

            // A method body physically declared in an open generic base is analyzed once, against
            // the unbound type parameter - a call site there references the base method's
            // OriginalDefinition (TData, not ConsumerPush), a different symbol than what chain's
            // GetMembers() (substituted through the closed base) produced above. ConstructedFrom
            // only unwraps a method's OWN type parameters, not its containing type's substitution -
            // verified empirically, it left this case unresolved.
            if (!SymbolEqualityComparer.Default.Equals(current, current.OriginalDefinition))
                map[current.OriginalDefinition] = effective;
        }

        return map;
    }

    public static IMethodSymbol Resolve(IMethodSymbol method, IReadOnlyDictionary<IMethodSymbol, IMethodSymbol> map) =>
        map.TryGetValue(method, out var effective) ? effective : method;
}
