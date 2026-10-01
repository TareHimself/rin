using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// A field assigned by a `with` expression and its Slang parameter name.
/// </summary>
internal sealed record WithHelperField(IFieldSymbol Field, string SlangName);

/// <summary>
/// A synthesized helper for one (type, assigned-field-set) `with` shape, e.g. `bounds with { Lower = x }`
/// becomes `bounds3DWithLower(Bounds3D self, float3 lower)`. A top-level function works in any expression
/// position, and `self` is a by-value copy, so it needs no [mutating].
/// </summary>
internal sealed record WithHelperSpec(ITypeSymbol Type, string FunctionName, List<WithHelperField> Fields);

/// <summary>
/// Finds the `with` expressions in method bodies and emits one helper function per distinct shape.
/// </summary>
internal static class WithLowering
{
    /// <summary>
    /// Records a helper spec, once per shape, for each `with` expression in the method's bodies.
    /// </summary>
    public static void Collect(IMethodSymbol method, Compilation compilation, List<Diagnostic> diagnostics,
        Dictionary<string, WithHelperSpec> specs, List<WithHelperSpec> order)
    {
        foreach (var body in MethodSource.GetBodies(method, compilation))
        foreach (var withOperation in FindWithOperations(body))
            CollectOne(withOperation, diagnostics, specs, order);
    }

    private static void CollectOne(IWithOperation withOperation, List<Diagnostic> diagnostics,
        Dictionary<string, WithHelperSpec> specs, List<WithHelperSpec> order)
    {
        var fields = ExtractFields(withOperation, diagnostics);
        if (fields is null) return;

        var type = withOperation.Type!;
        var key = ComputeKey(type, fields.Select(f => f.SlangName));
        if (specs.ContainsKey(key)) return;

        var functionName = $"{Naming.ToSlangIdentifier(type.Name)}With" +
                            string.Join("", fields.Select(f => Capitalize(f.SlangName)));
        var spec = new WithHelperSpec(type, functionName, fields);
        specs[key] = spec;
        order.Add(spec);
    }

    private static List<WithHelperField>? ExtractFields(IWithOperation withOperation, List<Diagnostic> diagnostics)
    {
        if (withOperation.Type is null || withOperation.Initializer is null)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                withOperation.Syntax.GetLocation(), "with expression"));
            return null;
        }

        var fields = new List<WithHelperField>();
        foreach (var initializer in withOperation.Initializer.Initializers)
        {
            if (initializer is not ISimpleAssignmentOperation { Target: IFieldReferenceOperation fieldRef })
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    initializer.Syntax.GetLocation(), "non-field member in a 'with' initializer"));
                return null;
            }

            fields.Add(new WithHelperField(fieldRef.Field, Naming.ToSlangIdentifier(fieldRef.Field.Name)));
        }

        fields.Sort((a, b) => string.CompareOrdinal(a.SlangName, b.SlangName));
        return fields;
    }

    /// <summary>
    /// A key identifying the (type, field set) shape, independent of field order.
    /// </summary>
    public static string ComputeKey(ITypeSymbol type, IEnumerable<string> slangFieldNames) =>
        $"{type.ToDisplayString()}|{string.Join(",", slangFieldNames.OrderBy(n => n, StringComparer.Ordinal))}";

    /// <summary>
    /// The Slang helper function that copies <c>self</c>, overwrites the fields and returns it.
    /// </summary>
    public static string Emit(WithHelperSpec spec)
    {
        var typeName = TypeMapping.MapType(spec.Type);
        var parameters = new List<string> { $"{typeName} self" };
        parameters.AddRange(spec.Fields.Select(f => $"{TypeMapping.MapType(f.Field.Type)} {f.SlangName}"));

        var writer = new SlangWriter();
        writer.OpenBrace($"{typeName} {spec.FunctionName}({string.Join(", ", parameters)})");
        foreach (var field in spec.Fields)
            writer.Line($"self.{field.SlangName} = {field.SlangName};");
        writer.Line("return self;");
        writer.CloseBrace();
        return writer.ToString();
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    // Stops at a local function, like FunctionCollector.FindReachableCallSites.
    private static IEnumerable<IWithOperation> FindWithOperations(IOperation root)
    {
        if (root is IWithOperation withOperation) yield return withOperation;
        if (root is ILocalFunctionOperation) yield break;

        foreach (var child in root.ChildOperations)
        foreach (var found in FindWithOperations(child))
            yield return found;
    }
}
