using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Resolves a method symbol's body operations, whichever declaration shape it came from (method,
/// constructor, local function or property accessor), so callers treat them all alike. An accessor's
/// syntax reference is an AccessorDeclarationSyntax, except for an expression-bodied property with no
/// accessor list (`public int Id => ...;`), where it is the bare ArrowExpressionClauseSyntax.
/// </summary>
internal static class MethodSource
{
    /// <summary>
    /// Whether the method has source with a body, as opposed to being abstract, extern or declared in metadata.
    /// </summary>
    public static bool HasBody(IMethodSymbol method) =>
        method.DeclaringSyntaxReferences.Any(r => HasBody(r.GetSyntax()));

    private static bool HasBody(SyntaxNode syntax) => syntax switch
    {
        MethodDeclarationSyntax { Body: not null } => true,
        MethodDeclarationSyntax { ExpressionBody: not null } => true,
        LocalFunctionStatementSyntax { Body: not null } => true,
        LocalFunctionStatementSyntax { ExpressionBody: not null } => true,
        ConstructorDeclarationSyntax { Body: not null } => true,
        ConstructorDeclarationSyntax { ExpressionBody: not null } => true,
        AccessorDeclarationSyntax { Body: not null } => true,
        AccessorDeclarationSyntax { ExpressionBody: not null } => true,
        ArrowExpressionClauseSyntax => true,
        _ => false
    };

    /// <summary>
    /// The body operation of each declaration of the method, in a normalized block or expression form.
    /// </summary>
    public static IEnumerable<IOperation> GetBodies(IMethodSymbol method, Compilation compilation)
    {
        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax();
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);

            switch (syntax)
            {
                case MethodDeclarationSyntax methodSyntax:
                    if (model.GetOperation(methodSyntax) is IMethodBodyOperation methodBody)
                    {
                        var body = methodBody.BlockBody ?? methodBody.ExpressionBody;
                        if (body is not null) yield return body;
                    }
                    break;
                case LocalFunctionStatementSyntax localFunctionSyntax:
                    if (model.GetOperation(localFunctionSyntax) is ILocalFunctionOperation { Body: { } localBody })
                        yield return localBody;
                    break;
                case ConstructorDeclarationSyntax constructorSyntax:
                    if (model.GetOperation(constructorSyntax) is IConstructorBodyOperation constructorBody)
                    {
                        var body = constructorBody.BlockBody ?? constructorBody.ExpressionBody;
                        if (body is not null) yield return body;
                    }
                    break;
                case AccessorDeclarationSyntax accessorSyntax:
                    if (model.GetOperation(accessorSyntax) is IMethodBodyOperation accessorBody)
                    {
                        var body = accessorBody.BlockBody ?? accessorBody.ExpressionBody;
                        if (body is not null) yield return body;
                    }
                    break;
                // GetOperation on the arrow clause already yields an implicit-return IBlockOperation.
                case ArrowExpressionClauseSyntax arrowSyntax:
                    if (model.GetOperation(arrowSyntax) is IBlockOperation arrowBody)
                        yield return arrowBody;
                    break;
            }
        }
    }
}
