using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using StageEntry = (Microsoft.CodeAnalysis.IMethodSymbol EffectiveMethod, Microsoft.CodeAnalysis.IMethodSymbol DeclSite,
    Microsoft.CodeAnalysis.AttributeData Marker);

namespace Rin.Shade.Transpiler;

/// <summary>
/// Lowers one shader class (and its base chain) to a complete Slang module: types, bindings, helper
/// functions and the compute or vertex/fragment entry points.
/// </summary>
internal sealed class ShaderLowering(Compilation compilation, List<Diagnostic> diagnostics)
{
    /// <summary>
    /// Returns the Slang source for the shader class, or null (after adding a diagnostic) if its entry points are invalid.
    /// </summary>
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
            // bindless set at set 0, so bindless blocks must be declared first.
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

        // Types seen only in a helper's signature or as a local in a reachable body are not yet in the
        // graph. Must run after functionOrder exists and before the graph is emitted.
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

        // With helpers call nothing else, so emitting them before functionOrder satisfies Slang's
        // define-before-use rule.
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

        var declared = TypeEmitter.Declared(types);
        TypeEmitter.Emit(types, diagnostics, writer, declared);

        // Most types the shader names live in its own namespace, so open it to keep signatures short.
        var shaderNamespace = Naming.NamespacePath(shaderClass);
        var usings = new List<string>();
        if (declared.Contains(shaderNamespace.Length == 0 ? shaderClass.Name : $"{shaderNamespace}::{shaderClass.Name}"))
            usings.Add(shaderNamespace.Length == 0 ? shaderClass.Name : $"{shaderNamespace}::{shaderClass.Name}");
        if (shaderNamespace.Length > 0 && declared.Contains(shaderNamespace)) usings.Add(shaderNamespace);

        foreach (var name in usings) writer.Line($"using namespace {name};");
        if (usings.Count > 0) writer.Line();

        using var globalScope = NameScope.WithUsings(usings, declared);

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

        INamedTypeSymbol? openExtension = null;
        foreach (var function in FunctionGrouping.Group(functionOrder, functionCollector.Dependencies))
        {
            var lowered = FunctionLowering.Lower(compilation, function, diagnostics, withHelperSpecs, overrides);
            if (string.IsNullOrEmpty(lowered.Text)) continue;

            if (!SymbolEqualityComparer.Default.Equals(lowered.ExtensionOf, openExtension))
            {
                if (openExtension is not null) writer.CloseBrace().Line();
                openExtension = lowered.ExtensionOf;
                if (openExtension is not null)
                {
                    using (NameScope.Qualified())
                        writer.OpenBrace($"extension {TypeMapping.MapType(openExtension)}");
                }
            }
            else if (openExtension is not null)
            {
                writer.Line();
            }

            if (openExtension is null)
            {
                writer.Append(lowered.Text);
                writer.Line();
            }
            else
            {
                writer.AppendBlock(lowered.Text);
            }
        }

        if (openExtension is not null) writer.CloseBrace().Line();

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
    /// Yields shaderClass and every base class up to (not including) Rin.Shade.Shader, most-derived first.
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
    /// Finds every effective (most-derived) method in the chain whose root declaration carries the marker
    /// attribute, with the attribute and the declaration that holds it. Returns all matches so callers can
    /// detect two methods claiming the same stage.
    /// </summary>
    /// <remarks>
    /// GetAttributes() on an override omits the base declaration's attributes, so the attribute is found by
    /// walking OverriddenMethod. An override that does not repeat the marker still inherits it.
    /// </remarks>
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
