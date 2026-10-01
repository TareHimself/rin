using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Reads the [SlangExpression] and [SlangStatement] attributes that bind a C# method directly to Slang text.
/// </summary>
internal static class IntrinsicBindings
{
    public static string? GetSlangExpressionTemplate(IMethodSymbol method) =>
        GetTemplate(method, "Rin.Shade.SlangExpressionAttribute");

    public static string? GetSlangStatementTemplate(IMethodSymbol method) =>
        GetTemplate(method, "Rin.Shade.SlangStatementAttribute");

    private static string? GetTemplate(IMethodSymbol method, string attributeFullName) =>
        method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attributeFullName)
            ?.ConstructorArguments.FirstOrDefault().Value as string;

    public static bool HasBinding(IMethodSymbol method) =>
        method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() is
            "Rin.Shade.SlangExpressionAttribute" or "Rin.Shade.SlangStatementAttribute");
}
