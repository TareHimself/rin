using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// The type arguments of one generic method instantiation (and of its containing generic types), applied to the
/// types and calls found in its body. A generic method is lowered once per instantiation it is called with, so
/// the body of `Pick&lt;T&gt;` bound against `T` is printed with `T` replaced by `float` for `Pick&lt;float&gt;`.
/// </summary>
internal sealed class TypeSubstitution
{
    private readonly Dictionary<ITypeParameterSymbol, ITypeSymbol> _map;

    private TypeSubstitution(Dictionary<ITypeParameterSymbol, ITypeSymbol> map)
    {
        _map = map;
    }

    /// <summary>
    /// The substitution for a method symbol, or null when it has no closed type arguments to apply.
    /// </summary>
    public static TypeSubstitution? For(IMethodSymbol method)
    {
        var map = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        AddPairs(map, method.OriginalDefinition.TypeParameters, method.TypeArguments);
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
            AddPairs(map, type.OriginalDefinition.TypeParameters, type.TypeArguments);

        return map.Count == 0 ? null : new TypeSubstitution(map);
    }

    private static void AddPairs(Dictionary<ITypeParameterSymbol, ITypeSymbol> map,
        IReadOnlyList<ITypeParameterSymbol> parameters, IReadOnlyList<ITypeSymbol> arguments)
    {
        for (var i = 0; i < parameters.Count && i < arguments.Count; i++)
            if (!SymbolEqualityComparer.Default.Equals(parameters[i], arguments[i]))
                map[parameters[i]] = arguments[i];
    }

    public ITypeSymbol Apply(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol parameter when _map.TryGetValue(parameter, out var argument) => argument,
        INamedTypeSymbol { IsGenericType: true } named when named.TypeArguments.Any(ContainsParameter) =>
            named.OriginalDefinition.Construct(named.TypeArguments.Select(Apply).ToArray()),
        _ => type
    };

    /// <summary>
    /// The call target with this instantiation's types filled in: `Mix&lt;T&gt;` called from `Lerp&lt;float&gt;` is
    /// `Mix&lt;float&gt;`, and `Wrapper&lt;T&gt;.Sum` called from a `Wrapper&lt;float&gt;` member is `Wrapper&lt;float&gt;.Sum`.
    /// </summary>
    public IMethodSymbol Apply(IMethodSymbol method)
    {
        if (ContainsParameter(method.ContainingType) &&
            Apply(method.ContainingType) is INamedTypeSymbol closedContainer &&
            closedContainer.GetMembers(method.Name).OfType<IMethodSymbol>().FirstOrDefault(candidate =>
                SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, method.OriginalDefinition)) is { } member)
            method = member;

        return method.IsGenericMethod && method.TypeArguments.Any(ContainsParameter)
            ? method.OriginalDefinition.Construct(method.TypeArguments.Select(Apply).ToArray())
            : method;
    }

    private bool ContainsParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol parameter => _map.ContainsKey(parameter),
        INamedTypeSymbol named => named.TypeArguments.Any(ContainsParameter) ||
                                  named.ContainingType is { } container && ContainsParameter(container),
        _ => false
    };
}
