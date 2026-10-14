using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// A member assigned by a `with` expression: its type, its Slang parameter name, and the setter to call when it
/// is a property with a setter body. A field or an auto property is written directly.
/// </summary>
internal sealed record WithHelperMember(ITypeSymbol Type, string SlangName, IMethodSymbol? Setter);

/// <summary>
/// A synthesized helper for one (type, assigned-member-set) `with` shape, e.g. `bounds with { Lower = x }`
/// becomes `bounds3DWithLower(Bounds3D self, float3 lower)`. A top-level function works in any expression
/// position, and `self` is a by-value copy, so it needs no [mutating].
/// </summary>
internal sealed record WithHelperSpec(ITypeSymbol Type, string FunctionName, List<WithHelperMember> Members);

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
        var members = ExtractMembers(withOperation, diagnostics);
        if (members is null) return;

        var type = withOperation.Type!;
        var key = ComputeKey(type, members.Select(m => m.SlangName));
        if (specs.ContainsKey(key)) return;

        var functionName = $"{Naming.ToSlangIdentifier(type.Name)}With" +
                           string.Join("", members.Select(m => Capitalize(m.SlangName)));
        var spec = new WithHelperSpec(type, functionName, members);
        specs[key] = spec;
        order.Add(spec);
    }

    /// <summary>
    /// The members a `with` initializer assigns, sorted by Slang name, or null (with a diagnostic) when one is
    /// not a field, an auto property or a property with a setter body.
    /// </summary>
    public static List<WithHelperMember>? ExtractMembers(IWithOperation withOperation, List<Diagnostic> diagnostics)
    {
        if (withOperation.Type is null || withOperation.Initializer is null)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                withOperation.Syntax.GetLocation(), "with expression"));
            return null;
        }

        var members = new List<WithHelperMember>();
        foreach (var initializer in withOperation.Initializer.Initializers)
        {
            if (initializer is not ISimpleAssignmentOperation assignment ||
                ToMember(assignment.Target) is not { } member)
            {
                diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                    initializer.Syntax.GetLocation(),
                    "'with' initializer that is not a field, an auto property or a property with a setter body"));
                return null;
            }

            members.Add(member);
        }

        members.Sort((a, b) => string.CompareOrdinal(a.SlangName, b.SlangName));
        return members;
    }

    private static WithHelperMember? ToMember(IOperation target) => target switch
    {
        IFieldReferenceOperation fieldRef =>
            new WithHelperMember(fieldRef.Field.Type, Naming.ToSlangIdentifier(fieldRef.Field.Name), null),
        IPropertyReferenceOperation { Property: var property } when StructMembers.IsAutoProperty(property) =>
            new WithHelperMember(property.Type, Naming.ToSlangIdentifier(property.Name), null),
        IPropertyReferenceOperation { Property: { SetMethod: { } setter } property } when MethodSource.HasBody(setter) =>
            new WithHelperMember(property.Type, Naming.ToSlangIdentifier(property.Name), setter),
        _ => null
    };

    /// <summary>
    /// A key identifying the (type, member set) shape, independent of member order.
    /// </summary>
    public static string ComputeKey(ITypeSymbol type, IEnumerable<string> slangMemberNames) =>
        $"{type.ToDisplayString()}|{string.Join(",", slangMemberNames.OrderBy(n => n, StringComparer.Ordinal))}";

    /// <summary>
    /// The Slang helper function that copies <c>self</c>, overwrites the members and returns it.
    /// </summary>
    public static string Emit(WithHelperSpec spec)
    {
        var typeName = TypeMapping.MapType(spec.Type);
        var parameters = new List<string> { $"{typeName} self" };
        parameters.AddRange(spec.Members.Select(m => $"{TypeMapping.MapType(m.Type)} {m.SlangName}"));

        var writer = new SlangWriter();
        writer.OpenBrace($"{typeName} {spec.FunctionName}({string.Join(", ", parameters)})");
        foreach (var member in spec.Members)
            writer.Line(member.Setter is { } setter
                ? $"self.{Naming.ToSlangMethodName(setter)}({member.SlangName});"
                : $"self.{member.SlangName} = {member.SlangName};");
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
