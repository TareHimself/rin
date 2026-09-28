using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

internal static class FunctionLowering
{
    public static string Lower(Compilation compilation, IMethodSymbol method, List<Diagnostic> diagnostics,
        IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null)
    {
        if (method.RefKind != RefKind.None)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.UnsupportedConstruct,
                method.Locations.FirstOrDefault() ?? Location.None, $"ref return on '{method.Name}'"));
            return "";
        }

        if (method.ContainingType.TypeKind == TypeKind.Extension)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.ExtensionMethodNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, method.Name));
            return "";
        }

        if (method.IsGenericMethod)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.Emitter.GenericNotSupported,
                method.Locations.FirstOrDefault() ?? Location.None, method.Name));
            return "";
        }

        var isConstructor = method.MethodKind == MethodKind.Constructor;
        var signature = isConstructor
            ? "__init"
            : $"{TypeMapping.MapType(method.ReturnType)} {Naming.ToSlangIdentifier(method.Name)}";

        var writer = new SlangWriter();

        if (!method.IsStatic && method.ContainingType.TypeKind == TypeKind.Struct)
        {
            // A Slang extension adds a member to an existing struct without touching its own
            // declaration - implicit `this` and bare field access work exactly like a member
            // declared inside the struct, and the call site (instance.method(args) / T(args) for a
            // constructor) is identical either way, so invocation-lowering never needs to know about
            // this distinction. Constructors don't need [mutating] (initializing fields is their
            // whole job); an ordinary method needs it exactly when its body actually writes a field
            // through the implicit `this` - checked precisely rather than always adding it, since
            // over-marking a genuinely read-only method wasn't verified to be harmless.
            var fieldNames = method.ContainingType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic && !f.IsImplicitlyDeclared)
                .Select(f => Naming.ToSlangIdentifier(f.Name))
                .ToHashSet();

            writer.OpenBrace($"extension {method.ContainingType.Name}");
            if (!isConstructor && WritesToImplicitThis(compilation, method)) writer.Line("[mutating]");
            WriteSignatureAndBody(compilation, method, diagnostics, writer, signature, fieldNames, withHelpers);
            writer.CloseBrace();
            return writer.ToString();
        }

        WriteSignatureAndBody(compilation, method, diagnostics, writer, signature, withHelpers: withHelpers);
        return writer.ToString();
    }

    public static void WriteSignatureAndBody(Compilation compilation, IMethodSymbol method,
        List<Diagnostic> diagnostics, SlangWriter writer, string signaturePrefix,
        IReadOnlySet<string>? shadowableFieldNames = null,
        IReadOnlyDictionary<string, WithHelperSpec>? withHelpers = null)
    {
        var parameterList = string.Join(", ", method.Parameters.Select(p =>
            $"{RefModifier(p.RefKind)}{TypeMapping.MapType(p.Type)} {Naming.ToSlangIdentifier(p.Name)}"));

        writer.OpenBrace($"{signaturePrefix}({parameterList})");

        var body = new BodyLowering(diagnostics, writer, shadowableFieldNames, withHelpers);
        foreach (var bodyOperation in MethodSource.GetBodies(method, compilation))
            body.LowerStatement(bodyOperation);

        writer.CloseBrace();
    }

    private static bool WritesToImplicitThis(Compilation compilation, IMethodSymbol method)
    {
        foreach (var body in MethodSource.GetBodies(method, compilation))
        foreach (var op in body.DescendantsAndSelf())
            if (op is ISimpleAssignmentOperation
                {
                    Target: IFieldReferenceOperation { Instance: IInstanceReferenceOperation }
                })
                return true;
        return false;
    }

    private static string RefModifier(RefKind refKind) => refKind switch
    {
        RefKind.Out => "out ",
        RefKind.Ref => "inout ",
        RefKind.In => "in ",
        _ => ""
    };
}
