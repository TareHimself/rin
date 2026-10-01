using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using StageEntry = (Microsoft.CodeAnalysis.IMethodSymbol EffectiveMethod, Microsoft.CodeAnalysis.IMethodSymbol DeclSite,
    Microsoft.CodeAnalysis.AttributeData Marker);

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

        var bindingFields = chain.SelectMany(t => t.GetMembers().OfType<IFieldSymbol>())
            .Where(BindingLowering.HasBindingGroupAttribute)
            .GroupBy(f => f.Name)
            .Select(g => g.First())
            // Slang numbers parameter blocks in declaration order, and the engine binds the global
            // bindless set once per frame at set 0 - so bindless blocks are declared first.
            .OrderBy(f => BindingLowering.BindlessBlockName(f) is null ? 1 : 0)
            .ToList();

        var computeEntries = FindStageEntries(chain, "Rin.Shade.ComputeAttribute");
        var vertexEntries = FindStageEntries(chain, "Rin.Shade.VertexAttribute");
        var fragmentEntries = FindStageEntries(chain, "Rin.Shade.FragmentAttribute");

        if (computeEntries.Count > 0 && (vertexEntries.Count > 0 || fragmentEntries.Count > 0))
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.ConflictingEntryPoints,
                shaderClass.Locations.FirstOrDefault() ?? Location.None, shaderClass.Name));
            return null;
        }

        if (fragmentEntries.Count > 0 && vertexEntries.Count == 0)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.FragmentWithoutVertex,
                shaderClass.Locations.FirstOrDefault() ?? Location.None, shaderClass.Name));
            return null;
        }

        if (computeEntries.Count == 0 && vertexEntries.Count == 0)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.NoEntryPoint,
                shaderClass.Locations.FirstOrDefault() ?? Location.None, shaderClass.Name));
            return null;
        }

        if (vertexEntries.Count > 1)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.DuplicateStageEntryPoint,
                shaderClass.Locations.FirstOrDefault() ?? Location.None, shaderClass.Name, "Vertex"));
            return null;
        }

        if (fragmentEntries.Count > 1)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.DuplicateStageEntryPoint,
                shaderClass.Locations.FirstOrDefault() ?? Location.None, shaderClass.Name, "Fragment"));
            return null;
        }

        var computeEntry = computeEntries.Count > 0 ? computeEntries[0] : (StageEntry?)null;
        var vertexEntry = vertexEntries.Count > 0 ? vertexEntries[0] : (StageEntry?)null;
        var fragmentEntry = fragmentEntries.Count > 0 ? fragmentEntries[0] : (StageEntry?)null;

        var entryMethods = new List<IMethodSymbol>();
        if (computeEntry is { } computeForCollection) entryMethods.Add(computeForCollection.EffectiveMethod);
        if (vertexEntry is { } vertexForCollection) entryMethods.Add(vertexForCollection.EffectiveMethod);
        if (fragmentEntry is { } fragmentForCollection) entryMethods.Add(fragmentForCollection.EffectiveMethod);

        var types = new TypeGraph();

        if (pushField is not null) types.Add(pushField.Type);
        foreach (var bindingField in bindingFields) types.Add(bindingField.Type);
        foreach (var entryMethod in entryMethods)
        {
            foreach (var parameter in entryMethod.Parameters)
                types.Add(parameter.Type);
            if (!entryMethod.ReturnsVoid)
                types.Add(entryMethod.ReturnType);
        }

        var overrides = OverrideResolution.Build(chain);

        var functionCollector = new FunctionCollector(compilation, diagnostics, overrides);
        foreach (var entryMethod in entryMethods) functionCollector.Collect(entryMethod);
        var entryMethodSet = new HashSet<IMethodSymbol>(entryMethods, SymbolEqualityComparer.Default);
        var functionOrder = functionCollector.Order
            .Where(m => !entryMethodSet.Contains(m))
            .ToList();

        // A helper's own signature, or a type introduced only as a local variable somewhere in a
        // reachable body, is otherwise invisible to the type graph - only the push field and entry-point
        // signatures were seeded above. Must run after functionOrder exists, but before the graph is
        // emitted below.
        foreach (var function in functionOrder)
        {
            foreach (var parameter in function.Parameters)
                types.Add(parameter.Type);
            if (!function.ReturnsVoid)
                types.Add(function.ReturnType);
        }

        foreach (var entryMethod in entryMethods)
            foreach (var body in MethodSource.GetBodies(entryMethod, compilation))
                types.AddFromBody(body);
        foreach (var function in functionOrder)
            foreach (var body in MethodSource.GetBodies(function, compilation))
                types.AddFromBody(body);

        // Helpers call nothing else, so emitting them ahead of functionOrder always satisfies
        // Slang's define-before-use requirement.
        var withHelperSpecs = new Dictionary<string, WithHelperSpec>();
        var withHelperOrder = new List<WithHelperSpec>();
        foreach (var entryMethod in entryMethods)
            WithLowering.Collect(entryMethod, compilation, diagnostics, withHelperSpecs, withHelperOrder);
        foreach (var function in functionOrder)
            WithLowering.Collect(function, compilation, diagnostics, withHelperSpecs, withHelperOrder);

        var writer = new SlangWriter();

        if (bindingFields.Any(f => BindingLowering.BindlessBlockName(f) is not null))
        {
            writer.Append(BindingLowering.BindlessAttributeDeclaration);
            writer.Line();
        }

        TypeEmitter.Emit(types, diagnostics, writer);

        if (bindingFields.Count > 0)
        {
            foreach (var field in bindingFields)
            {
                var declaration = BindingLowering.Lower(field, diagnostics);
                if (declaration is not null) writer.Line(declaration);
            }
            writer.Line();
        }

        foreach (var helper in withHelperOrder)
        {
            writer.Append(WithLowering.Emit(helper));
            writer.Line();
        }

        foreach (var function in functionOrder)
        {
            writer.Append(FunctionLowering.Lower(compilation, function, diagnostics, withHelperSpecs, overrides));
            writer.Line();
        }

        if (pushField is not null)
        {
            writer.Line(
                $"[[vk::push_constant]] uniform ConstantBuffer<{TypeMapping.MapType(pushField.Type)}, ScalarDataLayout> push;");
            writer.Line();
        }

        if (computeEntry is { } compute)
        {
            var (x, y, z) = GetNumThreads(compute.Marker);
            writer.Line("[shader(\"compute\")]");
            writer.Line($"[numthreads({x}, {y}, {z})]");

            FunctionLowering.WriteSignatureAndBody(compilation, compute.EffectiveMethod, diagnostics, writer,
                $"void {Naming.ToSlangIdentifier(compute.EffectiveMethod.Name)}", withHelpers: withHelperSpecs,
                overrides: overrides);
        }
        else
        {
            if (vertexEntry is { } vertex)
                EmitGraphicsStage(compilation, diagnostics, writer, "vertex", vertex, withHelperSpecs, overrides);

            if (fragmentEntry is { } fragment)
            {
                writer.Line();
                EmitGraphicsStage(compilation, diagnostics, writer, "fragment", fragment, withHelperSpecs, overrides);
            }
        }

        return writer.ToString();
    }

    private static void EmitGraphicsStage(Compilation compilation, List<Diagnostic> diagnostics, SlangWriter writer,
        string stageName, StageEntry entry,
        IReadOnlyDictionary<string, WithHelperSpec> withHelperSpecs,
        IReadOnlyDictionary<IMethodSymbol, IMethodSymbol> overrides)
    {
        writer.Line($"[shader(\"{stageName}\")]");

        var signature = entry.EffectiveMethod.ReturnsVoid
            ? $"void {Naming.ToSlangIdentifier(entry.EffectiveMethod.Name)}"
            : $"{TypeMapping.MapType(entry.EffectiveMethod.ReturnType)} {Naming.ToSlangIdentifier(entry.EffectiveMethod.Name)}";

        FunctionLowering.WriteSignatureAndBody(compilation, entry.EffectiveMethod, diagnostics, writer, signature,
            withHelpers: withHelperSpecs, overrides: overrides);
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
    /// Resolves every effective (most-derived) method in the chain whose root declaration carries
    /// the given marker attribute, plus that attribute instance itself and the exact declaration
    /// where it was found (which is what carries any state the attribute holds, e.g. [Compute]'s
    /// thread-group size - not necessarily anything on the effective method's own declaration).
    /// GetAttributes() on an override does NOT include the base declaration's attributes, so both
    /// have to be found by walking OverriddenMethod, not by re-checking the effective method
    /// directly - an override that doesn't repeat the marker still inherits the base's attribute
    /// correctly this way, instead of silently losing it. Returning every match (rather than just
    /// the first) lets callers detect two unrelated methods both claiming the same stage.
    /// </summary>
    private static List<StageEntry> FindStageEntries(List<INamedTypeSymbol> chain, string markerAttributeFullName)
    {
        var results = new List<StageEntry>();
        foreach (var method in OverrideResolution.EffectiveMethods(chain))
            if (FindMarkerAttribute(method, markerAttributeFullName) is { } found)
                results.Add((method, found.DeclSite, found.Attribute));
        return results;
    }

    private static (IMethodSymbol DeclSite, AttributeData Attribute)? FindMarkerAttribute(
        IMethodSymbol method, string attributeFullName)
    {
        for (var current = method; current is not null; current = current.OverriddenMethod)
        {
            var attribute = current.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
            if (attribute is not null) return (current, attribute);
        }
        return null;
    }
}
