using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rin.Core.Generators;

internal static class GeneratorUtils
{
    /// <summary>
    /// Checks whether any partial declaration of <paramref name="type"/> carries the <c>partial</c> modifier.
    /// </summary>
    public static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl)
            {
                if (typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a filesystem-safe, collision-resistant hint name for a generated source file
    /// from a type's fully-qualified name (handles generics, nested types, and namespace clashes).
    /// </summary>
    public static string GetSafeHintName(INamedTypeSymbol typeSymbol)
    {
        var fullyQualified = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var sb = new StringBuilder(fullyQualified.Length);
        foreach (var c in fullyQualified)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        return sb.ToString();
    }
}
