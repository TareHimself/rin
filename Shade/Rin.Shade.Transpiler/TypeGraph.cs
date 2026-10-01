using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

internal sealed class TypeNode(INamedTypeSymbol symbol)
{
    public INamedTypeSymbol Symbol { get; } = symbol;

    /// <summary>The types this one is declared in terms of, in field order.</summary>
    public List<TypeNode> Dependencies { get; } = [];

    /// <summary>
    /// Types declared inside this struct in C#, which Slang declares inside its body too - so the
    /// emitted source reads like the C# it came from.
    /// </summary>
    public List<TypeNode> Nested { get; } = [];

    public TypeNode? Parent { get; set; }

    public TypeNode TopLevel => Parent?.TopLevel ?? this;
}

/// <summary>
/// The struct and enum types a shader reaches, and what each depends on. Building the graph only
/// decides which types exist and how they relate; where and in what order they are printed is the
/// emitter's job (see TypeEmitter), so placement rules - ordering, namespaces, nesting - live in one
/// place instead of being baked into the walk.
/// </summary>
internal sealed class TypeGraph
{
    private readonly Dictionary<INamedTypeSymbol, TypeNode> _nodes = new(SymbolEqualityComparer.Default);
    private readonly List<TypeNode> _roots = [];

    /// <summary>
    /// Adds a type a shader mentions directly (the push field, an entry-point signature, a local)
    /// and, transitively, everything it is built from.
    /// </summary>
    public void Add(ITypeSymbol type)
    {
        if (Visit(type) is { } node && node.TopLevel is var top && !_roots.Contains(top)) _roots.Add(top);
    }

    /// <summary>
    /// A type introduced only as a local variable inside a method body is invisible to the field
    /// walk - FunctionCollector still finds and emits methods that operate on it (a constructor, a
    /// property getter), but nothing declares the type itself unless every body is walked too.
    /// Mirrors FunctionCollector.FindReachableCallSites's local-function boundary.
    /// </summary>
    public void AddFromBody(IOperation? body)
    {
        if (body is null) return;
        if (body is IVariableDeclaratorOperation declarator) Add(declarator.Symbol.Type);
        if (body is ILocalFunctionOperation) return;

        foreach (var child in body.ChildOperations) AddFromBody(child);
    }

    /// <summary>
    /// The top-level nodes with their dependencies before them (Slang has no forward declarations
    /// for structs or enums), in the order the shader first mentioned them. A nested type is emitted
    /// inside its container, so what it depends on outside the container is what the container
    /// depends on.
    /// </summary>
    public IReadOnlyList<TypeNode> Ordered()
    {
        var ordered = new List<TypeNode>();
        var placed = new HashSet<TypeNode>();

        void Place(TypeNode top)
        {
            if (!placed.Add(top)) return;
            foreach (var dependency in ExternalDependencies(top, top)) Place(dependency);
            ordered.Add(top);
        }

        foreach (var root in _roots) Place(root);
        return GroupByNamespace(ordered);
    }

    /// <summary>
    /// Reorders a valid dependency order so types of one namespace sit together where their dependencies
    /// allow it: after each type, the next one is the earliest still-unplaced type of the same namespace
    /// whose dependencies are already placed. A namespace is only reopened when a dependency forces it.
    /// </summary>
    private static IReadOnlyList<TypeNode> GroupByNamespace(List<TypeNode> ordered)
    {
        var dependencies = ordered.ToDictionary(node => node, node => ExternalDependencies(node, node).Distinct().ToList());
        var remaining = ordered.ToList();
        var placed = new HashSet<TypeNode>();
        var result = new List<TypeNode>();
        string? current = null;

        bool IsReady(TypeNode node) => dependencies[node].All(placed.Contains);

        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(node => IsReady(node) && Naming.NamespacePath(node.Symbol) == current)
                       ?? remaining.First(IsReady);

            remaining.Remove(next);
            placed.Add(next);
            result.Add(next);
            current = Naming.NamespacePath(next.Symbol);
        }

        return result;
    }

    private static IEnumerable<TypeNode> ExternalDependencies(TypeNode node, TypeNode top)
    {
        foreach (var dependency in node.Dependencies)
            if (dependency.TopLevel != top)
                yield return dependency.TopLevel;

        foreach (var nested in node.Nested)
        foreach (var dependency in ExternalDependencies(nested, top))
            yield return dependency;
    }

    private TypeNode? Visit(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType) return Visit(arrayType.ElementType);
        if (InlineArrays.TryGet(type, out var inlineElement, out _)) return Visit(inlineElement);
        if (type is not INamedTypeSymbol named) return null;

        if (TypeMapping.IsBuiltIn(named))
            return named is { Name: "BufferRef", TypeArguments.Length: 1 } ? Visit(named.TypeArguments[0]) : null;

        if (named.TypeKind is not (TypeKind.Struct or TypeKind.Enum)) return null;
        if (_nodes.TryGetValue(named, out var existing)) return existing;

        var node = new TypeNode(named);
        _nodes.Add(named, node);

        if (named.ContainingType is { TypeKind: TypeKind.Struct } container && Visit(container) is { } parent)
        {
            node.Parent = parent;
            parent.Nested.Add(node);
        }

        if (named.TypeKind == TypeKind.Struct)
            foreach (var member in StructMembers.Instance(named))
                if (Visit(member.Type) is { } dependency)
                    node.Dependencies.Add(dependency);

        return node;
    }
}
