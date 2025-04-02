using Rin.Shading.Ast.Nodes;

namespace Rin.Shading;

public class SlangTranspiler : ITranspiler
{
    public void Transpile(IEnumerable<INode> source, Stream output)
    {
        using var writer = new TextWriter(output);
        Transpile(source, writer);
    }

    private string TranspileType(IType type)
    {
        return type switch
        {
            BuiltInTypeNode builtInTypeNode => builtInTypeNode.Value,
            PointerNode pointerNode => $"{TranspileType(pointerNode.Type)}*",
            StructTypeNode structTypeNode => structTypeNode.Struct.Name,
            UnknownType unknownType => unknownType
                .TypeName, //throw new Exception($"Unknown type: {unknownType.TypeName}"),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private string TranspileDeclaration(DeclarationNode declaration)
    {
        return declaration switch
        {
            ParameterDeclarationNode node => (node.IsInput ? "in" : "out") + $" {TranspileType(node.Type)} {node.Name}",
            StructVariableDeclarationNode node => $"{TranspileType(node.Type)} {node.Name}" +
                                                  (node.Mapping.Length > 0 ? $" : {node.Mapping}" : string.Empty),
            VariableDeclarationNode node => $"{TranspileType(node.Type)} {node.Name}",
            _ => throw new ArgumentOutOfRangeException(nameof(declaration))
        };
    }

    private string TranspileExpressionList(INode[] expressions)
    {
        return expressions.Length == 0
            ? string.Empty
            : expressions.Aggregate("", (t, c) => t + " , " + TranspileExpression(c))[3..];
    }

    private string TranspileExpression(INode expression)
    {
        return expression switch
        {
            PointerAccessNode node => $"{TranspileExpression(node.Target)}->{TranspileExpression(node.Identifier)}",
            AccessNode node => $"{TranspileExpression(node.Target)}.{TranspileExpression(node.Identifier)}",
            ArrayLiteralNode node => $"{{ {TranspileExpressionList(node.Items)} }}",
            BooleanLiteralNode node => node.Value.ToString(),
            BreakNode node => "break",
            IType node => TranspileType(node),
            CallNode node => $"{TranspileExpression(node.Target)}( {TranspileExpressionList(node.Arguments)} )",
            ConditionalNode node =>
                $"{TranspileExpression(node.Condition)} ? {TranspileExpression(node.Left)} : {TranspileExpression(node.Right)}",
            ContinueNode node => "continue",
            DeclarationNode node => TranspileDeclaration(node),
            DecrementNode node => node.Before
                ? $"--{TranspileExpression(node.Expression)}"
                : $"{TranspileExpression(node.Expression)}--",
            DefineNode node => $"#define {node.Name} {TranspileExpression(node.Expression)}",
            DiscardNode node => "discard",
            FloatLiteralNode node => node.Value,
            IdentifierNode node => node.Value,
            IncludeNode node => $"#include \"{node.Path}\"",
            IncrementNode node => node.Before
                ? $"++{TranspileExpression(node.Expression)}"
                : $"{TranspileExpression(node.Expression)}++",
            IndexNode node => $"{TranspileExpression(node.Target)}[ {TranspileExpression(node.Expression)} ]",
            InjectNode node => node.Code,
            IntLiteralNode node => node.Value.ToString(),
            NegateNode node => $"-{TranspileExpression(node.Expression)}",
            NoOpNode node => string.Empty,
            NotNode node => $"!{TranspileExpression(node.Expression)}",
            BinaryOperatorNode node => node.Operator switch
            {
                BinaryOperator.Addition => $"{TranspileExpression(node.Left)} + {TranspileExpression(node.Right)}",
                BinaryOperator.Subtraction => $"{TranspileExpression(node.Left)} - {TranspileExpression(node.Right)}",
                BinaryOperator.Multiplication =>
                    $"{TranspileExpression(node.Left)} * {TranspileExpression(node.Right)}",
                BinaryOperator.Division => $"{TranspileExpression(node.Left)} / {TranspileExpression(node.Right)}",
                BinaryOperator.Assign => $"{TranspileExpression(node.Left)} = {TranspileExpression(node.Right)}",
                BinaryOperator.AdditionAssign =>
                    $"{TranspileExpression(node.Left)} += {TranspileExpression(node.Right)}",
                BinaryOperator.SubtractionAssign =>
                    $"{TranspileExpression(node.Left)} -= {TranspileExpression(node.Right)}",
                BinaryOperator.MultiplicationAssign =>
                    $"{TranspileExpression(node.Left)} *= {TranspileExpression(node.Right)}",
                BinaryOperator.DivisionAssign =>
                    $"{TranspileExpression(node.Left)} /= {TranspileExpression(node.Right)}",
                BinaryOperator.Equal => $"{TranspileExpression(node.Left)} == {TranspileExpression(node.Right)}",
                BinaryOperator.NotEqual => $"{TranspileExpression(node.Left)} != {TranspileExpression(node.Right)}",
                BinaryOperator.Less => $"{TranspileExpression(node.Left)} < {TranspileExpression(node.Right)}",
                BinaryOperator.LessEqual => $"{TranspileExpression(node.Left)} <= {TranspileExpression(node.Right)}",
                BinaryOperator.Greater => $"{TranspileExpression(node.Left)} > {TranspileExpression(node.Right)}",
                BinaryOperator.GreaterEqual => $"{TranspileExpression(node.Left)} >= {TranspileExpression(node.Right)}",
                BinaryOperator.And => $"{TranspileExpression(node.Left)} && {TranspileExpression(node.Right)}",
                BinaryOperator.Or => $"{TranspileExpression(node.Left)} || {TranspileExpression(node.Right)}",
                _ => throw new ArgumentOutOfRangeException()
            },
            PrecedenceNode node => $"( {TranspileExpression(node.Expression)} )",
            ReturnNode node => $"return {TranspileExpression(node.Expression)}",
            _ => throw new ArgumentOutOfRangeException(nameof(expression))
        };
    }

    private void TranspileStatement(INode statement, TextWriter output)
    {
        switch (statement)
        {
            case ScopeNode node:
                output.Write("{");
                output.AddTab();
                TranspileStatements(node.Statements, output);
                output.RemoveTab();
                output.Write("}");
                break;
            case IfNode node:
            {
                output.Write($"if({TranspileExpression(node.Condition)})");
                output.Write("{");
                output.AddTab();
                TranspileStatements(node.Scope.Children, output);
                output.RemoveTab();
                output.Write("}");
                if (node.Else is not null)
                {
                    output.Write("else");
                    TranspileStatement(node.Else, output);
                }
            }
                break;
            case ForNode node:
                output.Write(
                    $"for({TranspileExpression(node.Init)};{TranspileExpression(node.Condition)};{TranspileExpression(node.Update)})");
                output.Write("{");
                output.AddTab();
                TranspileStatements(node.Scope.Children, output);
                output.RemoveTab();
                output.Write("}");
                break;
            case InjectNode asInjected:
            {
                foreach (var line in asInjected.Code.Split('\n')) output.Write(line);
            }
                break;
            default:
                output.Write($"{TranspileExpression(statement)};");
                break;
        }
    }

    private void TranspileStatements(IEnumerable<INode> statements, TextWriter output)
    {
        foreach (var statement in statements) TranspileStatement(statement, output);
    }

    private string TranspileFunctionParameters(ParameterDeclarationNode[] parameters)
    {
        return parameters.Length == 0
            ? string.Empty
            : parameters.Aggregate("", (t, c) => t + " , " + TranspileDeclaration(c))[3..];
    }

    private void TranspileFunction(FunctionNode node, TextWriter output)
    {
        output
            .Write($"{TranspileType(node.ReturnType)} {node.Name}({TranspileFunctionParameters(node.Params)})")
            .Write("{")
            .AddTab();
        TranspileStatements(node.Scope.Statements, output);
        output
            .RemoveTab()
            .Write("}");
    }

    private void Transpile(IEnumerable<INode> source, TextWriter output)
    {
        foreach (var node in source)
            switch (node)
            {
                case FunctionNode asFunction:
                    TranspileFunction(asFunction, output);
                    break;
                case PushConstantNode asPushConstant:
                {
                    output.Write("struct PushConstant");
                    output.Write("{");
                    output.AddTab();
                    foreach (var declaration in asPushConstant.Declarations)
                        output.Write($"{TranspileDeclaration(declaration)};");
                    output.RemoveTab();
                    output.Write("};");
                    output.Write("[[vk::push_constant]]");
                    output.Write("uniform ConstantBuffer<PushConstant, ScalarDataLayout> push;");
                }
                    break;
                case StructNode asStruct:
                {
                    output.Write($"struct {asStruct.Name}");
                    output.Write("{");
                    output.AddTab();
                    foreach (var declaration in asStruct.Declarations)
                        output.Write($"{TranspileDeclaration(declaration)};");
                    output.RemoveTab();
                    output.Write("};");
                }
                    break;
                case InjectNode asInjected:
                {
                    foreach (var line in asInjected.Code.Split('\n')) output.Write(line);
                }
                    break;
            }
    }
}