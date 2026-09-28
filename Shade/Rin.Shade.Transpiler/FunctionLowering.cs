using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class FunctionLowering
{
    public static string Lower(Compilation compilation, IMethodSymbol method, List<Diagnostic> diagnostics)
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

        var writer = new SlangWriter();
        var signature = $"{TypeMapping.MapType(method.ReturnType)} {Naming.ToSlangIdentifier(method.Name)}";

        if (!method.IsStatic && method.ContainingType.TypeKind == TypeKind.Struct)
        {
            // A Slang extension adds a method to an existing struct without touching its own
            // declaration - implicit `this` and bare field access work exactly like a method
            // declared inside the struct, and the call site (instance.method(args)) is identical
            // either way, so invocation-lowering never needs to know about this distinction.
            writer.OpenBrace($"extension {method.ContainingType.Name}");
            WriteSignatureAndBody(compilation, method, diagnostics, writer, signature);
            writer.CloseBrace();
            return writer.ToString();
        }

        WriteSignatureAndBody(compilation, method, diagnostics, writer, signature);
        return writer.ToString();
    }

    public static void WriteSignatureAndBody(Compilation compilation, IMethodSymbol method,
        List<Diagnostic> diagnostics, SlangWriter writer, string signaturePrefix)
    {
        var parameterList = string.Join(", ", method.Parameters.Select(p =>
            $"{RefModifier(p.RefKind)}{TypeMapping.MapType(p.Type)} {Naming.ToSlangIdentifier(p.Name)}"));

        writer.OpenBrace($"{signaturePrefix}({parameterList})");

        var body = new BodyLowering(diagnostics, writer);
        foreach (var bodyOperation in MethodSource.GetBodies(method, compilation))
            body.LowerStatement(bodyOperation);

        writer.CloseBrace();
    }

    private static string RefModifier(RefKind refKind) => refKind switch
    {
        RefKind.Out => "out ",
        RefKind.Ref => "inout ",
        RefKind.In => "in ",
        _ => ""
    };
}
