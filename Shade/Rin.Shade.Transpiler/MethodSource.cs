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
/// A property/indexer accessor's own DeclaringSyntaxReferences point at an AccessorDeclarationSyntax
/// for a block/expression-bodied accessor (`get { ... }` / `get => ...;`), but at the bare
/// ArrowExpressionClauseSyntax for an expression-bodied property with no accessor list at all
/// (`public int Id => ...;`) - confirmed empirically, not documented behavior worth assuming.
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
        ConstructorDeclarationSyntax { Body: not null } => true,
        ConstructorDeclarationSyntax { ExpressionBody: not null } => true,
        AccessorDeclarationSyntax { Body: not null } => true,
        AccessorDeclarationSyntax { ExpressionBody: not null } => true,
        ArrowExpressionClauseSyntax => true,
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
                // `public int Id => ...;` - GetOperation on the arrow clause itself already returns
                // a normalized IBlockOperation (an implicit return wrapping the expression), the same
                // shape IMethodBodyOperation.ExpressionBody gives for every other member kind.
                case ArrowExpressionClauseSyntax arrowSyntax:
                    if (model.GetOperation(arrowSyntax) is IBlockOperation arrowBody)
                        yield return arrowBody;
                    break;
            }
        }
    }
}
