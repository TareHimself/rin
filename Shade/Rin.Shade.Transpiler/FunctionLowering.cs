using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// A lowered function. An instance method of a struct is an `extension` member: <see cref="ExtensionOf"/>
/// names the struct and <see cref="Text"/> is just the member, so neighbouring members of one struct can
/// share a single extension block. Anything else is a free function, printed as is.
/// </summary>
internal readonly record struct LoweredFunction(INamedTypeSymbol? ExtensionOf, string Text);

/// <summary>
/// Lowers a C# method, constructor or local function to a Slang function.
/// </summary>
internal static class FunctionLowering
{
    /// <summary>
    /// Lowers the method, or reports a diagnostic and returns a default value if it uses a construct that
    /// cannot be emitted (ref return, extension method, generic method).
    /// </summary>
    public static LoweredFunction Lower(Compilation compilation, IMethodSymbol method, List<Diagnostic> diagnostics,
        IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null,
        IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides = null)
    {
        if (method.RefKind != RefKind.None)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                method.Locations.FirstOrDefault() ?? Location.None, $"ref return on '{method.Name}'"));
            return default;
        }

        if (method.ContainingType.TypeKind == TypeKind.Extension)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.ExtensionMethodNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, method.Name));
            return default;
        }

        if (method.IsGenericMethod)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.GenericNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, method.Name));
            return default;
        }

        var isConstructor = method.MethodKind == MethodKind.Constructor;
        var signature = isConstructor
            ? "__init"
            : $"{TypeMapping.MapType(method.ReturnType)} {Naming.ToSlangMethodName(method)}";

        var writer = new SlangWriter();

        if (!method.IsStatic && method.ContainingType.TypeKind == TypeKind.Struct)
        {
            // An extension member behaves like one declared in the struct, and call sites look the same
            // either way. Constructors never need [mutating]; other methods get it only when the body
            // writes through the implicit `this`, since over-marking a read-only method is unverified.
            var fieldNames = method.ContainingType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic && !f.IsImplicitlyDeclared)
                .Select(f => Naming.ToSlangIdentifier(f.Name))
                .ToHashSet();

            if (!isConstructor && WritesToImplicitThis(compilation, method)) writer.Line("[mutating]");
            WriteSignatureAndBody(compilation, method, diagnostics, writer, signature, fieldNames, withHelpers,
                overrides);
            return new LoweredFunction(method.ContainingType, writer.ToString());
        }

        WriteSignatureAndBody(compilation, method, diagnostics, writer, signature, withHelpers: withHelpers,
            overrides: overrides);
        return new LoweredFunction(null, writer.ToString());
    }

    /// <summary>
    /// Writes <c>signaturePrefix(parameters)</c> followed by the braced lowered body.
    /// </summary>
    public static void WriteSignatureAndBody(Compilation compilation, IMethodSymbol method,
        List<Diagnostic> diagnostics, SlangWriter writer, string signaturePrefix,
        IReadOnlySet<string>? shadowableFieldNames = null,
        IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null,
        IReadOnlyDictionary<IMethodSymbol, IMethodSymbol>? overrides = null)
    {
        foreach (var arrayType in method.Parameters.Select(p => p.Type).Append(method.ReturnType)
                     .OfType<IArrayTypeSymbol>())
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedType,
                method.Locations.FirstOrDefault() ?? Location.None,
                $"{arrayType.ToDisplayString()} on '{method.Name}' (declare an [InlineArray] struct instead)"));

        var parameterList = string.Join(", ", method.Parameters.Select(p =>
            $"{RefModifier(p.RefKind)}{TypeMapping.MapType(p.Type)} {Naming.ToSlangIdentifier(p.Name)}"));

        writer.OpenBrace($"{signaturePrefix}({parameterList})");

        var body = new BodyLowering(diagnostics, writer, shadowableFieldNames, withHelpers, overrides);
        foreach (var bodyOperation in MethodSource.GetBodies(method, compilation))
            body.LowerStatement(bodyOperation);

        writer.CloseBrace();
    }

    private static bool WritesToImplicitThis(Compilation compilation, IMethodSymbol method)
    {
        foreach (var body in MethodSource.GetBodies(method, compilation))
        foreach (var op in body.DescendantsAndSelf())
            if (op is ISimpleAssignmentOperation { Target: var target } && RootedAtImplicitThis(target))
                return true;
        return false;
    }

    // `_locationU.X = v` (field of a field) and `Location = v` (setter) mutate the struct just like `Field = v`.
    private static bool RootedAtImplicitThis(IOperation target) => target switch
    {
        IFieldReferenceOperation { Instance: IInstanceReferenceOperation } => true,
        IFieldReferenceOperation { Instance: { } instance } => RootedAtImplicitThis(instance),
        IPropertyReferenceOperation { Instance: IInstanceReferenceOperation } => true,
        _ => false
    };

    private static string RefModifier(RefKind refKind) => refKind switch
    {
        RefKind.Out => "out ",
        RefKind.Ref => "inout ",
        RefKind.In => "in ",
        _ => ""
    };
}
