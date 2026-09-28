using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

internal static class IntrinsicBindings
{
    public static string? GetSlangCallTemplate(IMethodSymbol method) =>
        GetTemplate(method, "Rin.Shade.SlangCallAttribute");

    public static string? GetSlangStatementTemplate(IMethodSymbol method) =>
        GetTemplate(method, "Rin.Shade.SlangStatementAttribute");

    private static string? GetTemplate(IMethodSymbol method, string attributeFullName) =>
        method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName)
            ?.ConstructorArguments.FirstOrDefault().Value as string;

    public static bool HasBinding(IMethodSymbol method) =>
        method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() is
            "Rin.Shade.SlangCallAttribute" or "Rin.Shade.SlangStatementAttribute" or "Rin.Shade.SlangBodyAttribute");
}
