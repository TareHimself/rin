using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

internal sealed record WithHelperField(IFieldSymbol Field, string SlangName);

/// <summary>
/// A synthesized helper for one (type, overridden-field-set) shape of `with` expression, e.g.
/// `bounds with { Lower = x }` - "updatableBounds3DWithLower(UpdatableBounds3D self, float3 lower)".
/// Slang already copies a struct on parameter pass (the same value semantics [mutating] exists to
/// work around for instance methods), so `self` is a free, independent copy - the helper just
/// overwrites the named fields on it and returns it. Emitted as a plain top-level function, not a
/// struct extension member: mutating `self` (an ordinary by-value parameter, not an implicit `this`)
/// needs no [mutating], and a plain function composes into any expression position, unlike the
/// inline-hoisted-statement alternative that would only be safe where the expression is evaluated
/// exactly once.
/// </summary>
internal sealed record WithHelperSpec(ITypeSymbol Type, string FunctionName, List<WithHelperField> Fields);

internal static class WithLowering
{
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

    public static string ComputeKey(ITypeSymbol type, IEnumerable<string> slangFieldNames) =>
        $"{type.ToDisplayString()}|{string.Join(",", slangFieldNames.OrderBy(n => n, StringComparer.Ordinal))}";

    public static string Emit(WithHelperSpec spec)
    {
        var typeName = spec.Type.Name;
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

    // Mirrors FunctionCollector.FindReachableCallSites - stop at a local function boundary, since
    // its body is only ever walked (via its own entry in functionOrder) once something calls it.
    private static IEnumerable<IWithOperation> FindWithOperations(IOperation root)
    {
        if (root is IWithOperation withOperation) yield return withOperation;
        if (root is ILocalFunctionOperation) yield break;

        foreach (var child in root.ChildOperations)
        foreach (var found in FindWithOperations(child))
            yield return found;
    }
}
