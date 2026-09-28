using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Rin.Shade.Transpiler;

/// <summary>
/// Resolves a method symbol's body, whichever declaration shape it came from. A local function
/// ("inlined" per the v1 subset) declares via LocalFunctionStatementSyntax/ILocalFunctionOperation
/// instead of MethodDeclarationSyntax/IMethodBodyOperation, but is otherwise walked and emitted
/// exactly like any other plain function - this is the one place both shapes get normalized.
/// </summary>
internal static class MethodSource
{
    public static bool HasBody(IMethodSymbol method) =>
        method.DeclaringSyntaxReferences.Any(r => HasBody(r.GetSyntax()));

    private static bool HasBody(SyntaxNode syntax) => syntax switch
    {
        MethodDeclarationSyntax { Body: not null } => true,
        MethodDeclarationSyntax { ExpressionBody: not null } => true,
        LocalFunctionStatementSyntax { Body: not null } => true,
        LocalFunctionStatementSyntax { ExpressionBody: not null } => true,
        _ => false
    };

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
            }
        }
    }
}
