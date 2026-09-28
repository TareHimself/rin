using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal sealed class ShaderLowering(Compilation compilation, List<Diagnostic> diagnostics)
{
    public string? Lower(INamedTypeSymbol shaderClass)
    {
        if (shaderClass.IsGenericType)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                shaderClass.Locations.FirstOrDefault() ?? Location.None,
                $"generic shader class '{shaderClass.Name}'"));
            return null;
        }

        var chain = WalkBaseChain(shaderClass).ToList();

        var pushField = chain.SelectMany(t => t.GetMembers().OfType<IFieldSymbol>())
            .FirstOrDefault(f => HasAttribute(f, "Rin.Shade.PushAttribute"));

        var (entryMethod, computeAttribute) = ResolveEntryMethod(chain);

        if (entryMethod is null || computeAttribute is null)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.NoEntryPoint,
                shaderClass.Locations.FirstOrDefault() ?? Location.None, shaderClass.Name));
            return null;
        }

        var typeOrder = new List<INamedTypeSymbol>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        if (pushField is not null) TypeCollector.Collect(pushField.Type, typeOrder, visited);
        foreach (var parameter in entryMethod.Parameters)
            TypeCollector.Collect(parameter.Type, typeOrder, visited);

        var functionCollector = new FunctionCollector(compilation, diagnostics);
        functionCollector.Collect(entryMethod);
        var functionOrder = functionCollector.Order
            .Where(m => !SymbolEqualityComparer.Default.Equals(m, entryMethod))
            .ToList();

        // Walks the exact same bodies FunctionLowering/BodyLowering go on to render below, so every
        // `with` expression that ends up emitted has already had its helper collected here first -
        // helpers are leaf functions (no calls of their own), so emitting them ahead of functionOrder
        // always satisfies Slang's define-before-use requirement regardless of which function uses one.
        var withHelperSpecs = new Dictionary<string, WithHelperSpec>();
        var withHelperOrder = new List<WithHelperSpec>();
        WithLowering.Collect(entryMethod, compilation, diagnostics, withHelperSpecs, withHelperOrder);
        foreach (var function in functionOrder)
            WithLowering.Collect(function, compilation, diagnostics, withHelperSpecs, withHelperOrder);

        var writer = new SlangWriter();

        foreach (var type in typeOrder)
        {
            if (type.IsGenericType)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.GenericNotSupported,
                    type.Locations.FirstOrDefault() ?? Location.None, type.Name));
                continue;
            }

            var text = type.TypeKind == TypeKind.Enum
                ? EnumLowering.Lower(type, diagnostics)
                : StructLowering.Lower(type, diagnostics);
            writer.Append(text);
            writer.Line();
        }

        foreach (var helper in withHelperOrder)
        {
            writer.Append(WithLowering.Emit(helper));
            writer.Line();
        }

        foreach (var function in functionOrder)
        {
            writer.Append(FunctionLowering.Lower(compilation, function, diagnostics, withHelperSpecs));
            writer.Line();
        }

        if (pushField is not null)
        {
            writer.Line(
                $"[[vk::push_constant]] uniform ConstantBuffer<{pushField.Type.Name}, ScalarDataLayout> push;");
            writer.Line();
        }

        var (x, y, z) = GetNumThreads(computeAttribute);
        writer.Line("[shader(\"compute\")]");
        writer.Line($"[numthreads({x}, {y}, {z})]");

        FunctionLowering.WriteSignatureAndBody(compilation, entryMethod, diagnostics, writer,
            $"void {Naming.ToSlangIdentifier(entryMethod.Name)}", withHelpers: withHelperSpecs);

        return writer.ToString();
    }

    private static (int X, int Y, int Z) GetNumThreads(AttributeData computeAttribute)
    {
        if (computeAttribute.ConstructorArguments.Length != 3) return (1, 1, 1);
        return ((int)computeAttribute.ConstructorArguments[0].Value!,
            (int)computeAttribute.ConstructorArguments[1].Value!,
            (int)computeAttribute.ConstructorArguments[2].Value!);
    }

    private static bool HasAttribute(ISymbol symbol, string fullName) =>
        symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == fullName);

    /// <summary>
    /// shaderClass and every base class up to (not including) Rin.Shade.Shader, most-derived first.
    /// </summary>
    private IEnumerable<INamedTypeSymbol> WalkBaseChain(INamedTypeSymbol shaderClass)
    {
        var shaderBaseType = compilation.GetTypeByMetadataName("Rin.Shade.Shader");
        for (var current = shaderClass;
             current is not null && !SymbolEqualityComparer.Default.Equals(current, shaderBaseType);
             current = current.BaseType)
            yield return current;
    }

    /// <summary>
    /// Resolves the effective (most-derived) implementation for the one method in the chain whose
    /// root declaration carries [Compute], plus that attribute instance itself (which is what
    /// carries the actual thread-group size, not necessarily anything on the effective method's own
    /// declaration). GetAttributes() on an override does NOT include the base declaration's
    /// attributes, so both have to be found by walking OverriddenMethod, not by re-checking the
    /// effective method directly - an override that doesn't repeat [Compute(...)] still inherits the
    /// base's thread-group size correctly this way, instead of silently defaulting to (1,1,1).
    /// </summary>
    private static (IMethodSymbol? Method, AttributeData? ComputeAttribute) ResolveEntryMethod(
        List<INamedTypeSymbol> chain)
    {
        var allMethods = chain.SelectMany(t => t.GetMembers().OfType<IMethodSymbol>()).ToList();
        var shadowed = new HashSet<IMethodSymbol>(
            allMethods.Select(m => m.OverriddenMethod).Where(m => m is not null)!,
            SymbolEqualityComparer.Default);

        foreach (var method in allMethods.Where(m => !shadowed.Contains(m)))
        {
            var computeAttribute = FindComputeAttribute(method);
            if (computeAttribute is not null) return (method, computeAttribute);
        }

        return (null, null);
    }

    private static AttributeData? FindComputeAttribute(IMethodSymbol method)
    {
        for (var current = method; current is not null; current = current.OverriddenMethod)
        {
            var attribute = current.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Rin.Shade.ComputeAttribute");
            if (attribute is not null) return attribute;
        }
        return null;
    }
}
