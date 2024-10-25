using rsl.Nodes;


namespace rsl.Generator;

public class GlslGenerator : Generator
{
    private readonly Dictionary<int, string> ComputedTabs = new();
    private Mutex TabsMutex = new();


    public virtual string Tabs(int depth)
    {
        lock (ComputedTabs)
        {
            if (ComputedTabs.TryGetValue(depth, out var tabs)) return tabs;

            var result = "";

            for (var i = 0; i < depth; i++) result += '\t';

            ComputedTabs.Add(depth, result);
            return result;
        }
    }

    public virtual string GetDeclarationType(DeclarationNode node)
    {
        return node.DeclarationType switch
        {
            DeclarationType.Struct => node is StructDeclarationNode structDeclarationNode
                ? structDeclarationNode.StructName
                : throw new Exception("Declaration type is struct but node is not StructNode"),
            DeclarationType.Block => node is BlockDeclarationNode blockDeclarationNode
                ? blockDeclarationNode.GetTypeName()
                : throw new Exception("Declaration type is block but node is not BlockNode"),
            DeclarationType.Float => "float",
            DeclarationType.Int => "int",
            DeclarationType.Float2 => "vec2",
            DeclarationType.Int2 => "ivec2",
            DeclarationType.Float3 => "vec3",
            DeclarationType.Int3 => "ivec3",
            DeclarationType.Float4 => "vec4",
            DeclarationType.Int4 => "ivec4",
            DeclarationType.Mat3 => "mat3",
            DeclarationType.Mat4 => "mat4",
            DeclarationType.Void => "void",
            DeclarationType.Buffer => "buffer",
            DeclarationType.Sampler2D => "sampler2D",
            DeclarationType.Boolean => "bool",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual string GenerateDeclaration(DeclarationNode node,int depth)
    {
        if (node is BlockDeclarationNode asBlockDeclaration)
        {
            return $"{GetDeclarationType(node)} " + "{\n" + asBlockDeclaration.Declarations.Aggregate("",(t,nodDeclaration) => t + Tabs(depth + 1) + GenerateDeclaration(nodDeclaration,depth) + ";\n") + Tabs(depth) + "}" + $" {node.DeclarationName}"+ node.Count switch
            {
                -1 => "[]",
                1 => "",
                _ => $"[{node.Count}]"
            };
        }

        if (node is BufferDeclarationNode asBufferDeclaration)
        {
            return $"{GetDeclarationType(node)} {node.DeclarationName}" + "{\n" + asBufferDeclaration.Declarations.Aggregate("",(t,nodDeclaration) => t + Tabs(depth + 1) + GenerateDeclaration(nodDeclaration,depth) + ";\n") + Tabs(depth) + "}";
        }
        
        return $"{GetDeclarationType(node)} {node.DeclarationName}" + node.Count switch
        {
            -1 => "[]",
            1 => "",
            _ => $"[{node.Count}]"
        };
    }

    public virtual string GenerateFunctionArgument(FunctionArgumentNode node)
    {
        return (node.IsInput ? "in " : "out ") + GetDeclarationType(node.Declaration) +
               (node.Declaration.Count > 1 ? $"[{node.Declaration.Count}]" : "") + $" {node.Declaration.DeclarationName}";
    }

    public virtual string GenerateExpression(Node expression)
    {
        switch (expression.NodeType)
        {
            case NodeType.BinaryOp:
            {
                if (expression is BinaryOpNode node)
                    return $"{GenerateExpression(node.Left)} " + node.Op switch
                        {
                            BinaryOp.Multiply => "*",
                            BinaryOp.Divide => "/",
                            BinaryOp.Add => "+",
                            BinaryOp.Subtract => "-",
                            BinaryOp.Mod => "%",
                            BinaryOp.And => "&&",
                            BinaryOp.Or => "||",
                            BinaryOp.Not => "!",
                            BinaryOp.Equal => "==",
                            BinaryOp.NotEqual => "!=",
                            BinaryOp.Less => "<",
                            BinaryOp.LessEqual => "<=",
                            BinaryOp.Greater => ">",
                            BinaryOp.GreaterEqual => ">=",
                            _ => throw new ArgumentOutOfRangeException()
                        } + $" {GenerateExpression(node.Right)}";
            }
                goto default;
            case NodeType.Return:
            {
                if (expression is ReturnNode node) return $"return {GenerateExpression(node.Expression)}";
            }
                goto default;
            case NodeType.Assign:
            {
                if (expression is AssignNode node) return GenerateAssign(node);
            }
                goto default;
            case NodeType.Call:
            {
                if (expression is CallNode node)
                {
                    var arguments = "";
                    foreach (var nodeArgument in node.Arguments)
                    {
                        arguments += GenerateExpression(nodeArgument);

                        if (nodeArgument != node.Arguments.Last()) arguments += " , ";
                    }

                    return $"{GenerateExpression(node.Identifier)}( {arguments} )";
                }
            }
                goto default;
            case NodeType.Access:
            {
                if (expression is AccessNode node)
                    return $"{GenerateExpression(node.Left)}.{GenerateExpression(node.Right)}";
            }
                goto default;
            case NodeType.Index:
            {
                if (expression is IndexNode node)
                    return $"{GenerateExpression(node.Left)}[{GenerateExpression(node.IndexExpression)}]";
            }
                goto default;
            case NodeType.Identifier:
            {
                if (expression is IdentifierNode node)
                {
                    var tokenType = Token.KeywordToTokenType(node.Identity);
                    
                    if (tokenType == null) return $"{node.Identity}";

                    return tokenType switch
                    {
                        TokenType.TypeFloat2 => "vec2",
                        TokenType.TypeFloat3 => "vec3",
                        TokenType.TypeFloat4 => "vec4",
                        TokenType.TypeInt2 => "ivec2",
                        TokenType.TypeInt3 => "ivec3",
                        TokenType.TypeInt4 => "ivec4",
                        _ => $"{node.Identity}"
                    };
                }
            }
                goto default;
            case NodeType.Declaration:
            {
                if (expression is DeclarationNode node) return GenerateDeclaration(node,0);
            }
                goto default;
            case NodeType.Include:
            {
                if (expression is IncludeNode node) return $"#include \"{node.File}\"";
            }
                goto default;
            case NodeType.FloatLiteral:
            {
                if (expression is FloatLiteral node)
                {
                    var result = $"{node.Value}";
                    if (!result.Contains('.')) result += ".0";
                    return result;
                }
            }
                goto default;
            case NodeType.IntLiteral:
            {
                if (expression is IntLiteral node) return $"{node.Value}";
            }
                goto default;
            case NodeType.Const:
            {
                if (expression is ConstNode node) return $"const {GenerateDeclaration(node.Declaration,0)}";
            }
                goto default;
            case NodeType.ArrayLiteral:
            {
                if (expression is ArrayLiteralNode node)
                {
                    var arrayStr = "{ ";
                    foreach (var nodeExpression in node.Expressions)
                    {
                        arrayStr += GenerateExpression(nodeExpression);
                        if (nodeExpression != node.Expressions.Last()) arrayStr += " , ";
                    }

                    return arrayStr + " }";
                }
            }
                goto default;
            case NodeType.Negate:
            {
                if (expression is NegateNode node) return $"-{GenerateExpression(node.Expression)}";
            }
                goto default;
            case NodeType.Precedence:
            {
                if (expression is PrecedenceNode node) return $"( {GenerateExpression(node.Expression)} )";
            }
                goto default;
            case NodeType.Increment:
            {
                if (expression is IncrementNode node)
                    return node.IsPre ? $"++{GenerateExpression(node.Target)}" : $"{GenerateExpression(node.Target)}++";
            }
                goto default;
            case NodeType.Decrement:
            {
                if (expression is DecrementNode node)
                    return node.IsPre ? $"--{GenerateExpression(node.Target)}" : $"{GenerateExpression(node.Target)}--";
            }
                goto default;
            case NodeType.Discard:
            {
                if (expression is DiscardNode node) return "discard";
            }
                goto default;
            case NodeType.Conditional:
            {
                if (expression is ConditionalNode node) return $"{GenerateExpression(node.Condition)} ? {GenerateExpression(node.Left)} : {GenerateExpression(node.Right)}";
            }
                goto default;
            case NodeType.BinaryOpAndAssign:
            {
                if (expression is BinaryOpAndAssignNode node) return GenerateBinaryOpAndAssign(node);
            }
                goto default;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual string GenerateStatement(Node statement,int depth)
    {
        return $"{Tabs(depth)}{GenerateExpression(statement)};";
    }

    public virtual string GenerateFor(ForNode node,int depth)
    {
        return $"{Tabs(depth)}for({GenerateExpression(node.Initial)};{GenerateExpression(node.Condition)};{GenerateExpression(node.Update)})\n" + GenerateScope(node.Scope,depth);
    }
    
    public virtual string GenerateIf(IfNode node,int depth)
    {
        var result = $"{Tabs(depth)}if({GenerateExpression(node.Condition)})" + GenerateScope(node.Scope,depth);

        if (node.Else is NoOpNode) return result;

        return result + $"{Tabs(depth)}else " + (node.Else is ScopeNode
            ? GenerateScope((ScopeNode)node.Else,depth)
            : "\n" + GenerateIf((IfNode)node.Else,depth));
    }

    public virtual string GenerateScope(ScopeNode node,int depth)
    {
        var result = "\n" + Tabs(depth) + "{\n";
        foreach (var nodeStatement in node.Statements)
        {
            if (nodeStatement is ScopeNode nestedScope)
            {
                result += GenerateScope(nestedScope,depth + 1);
                continue;
            }

            if (nodeStatement is ForNode forNode)
            {
                result += GenerateFor(forNode,depth + 1);
                continue;
            }
            
            if (nodeStatement is IfNode ifNode)
            {
                result += GenerateIf(ifNode,depth + 1);
                continue;
            }

            result += GenerateStatement(nodeStatement,depth + 1) + "\n";
        }

        return result + Tabs(depth) + "}\n";
    }

    public virtual string GenerateFunction(FunctionNode node,int depth)
    {
        var result = $"{Tabs(depth)}{GenerateDeclaration(node.ReturnDeclaration,depth)}{node.Name}(";
        foreach (var functionArgumentNode in node.Arguments)
        {
            result += GenerateFunctionArgument(functionArgumentNode);
            if (functionArgumentNode != node.Arguments.Last()) result += " , ";
        }

        result+= ")";
        
        return result + GenerateScope(node.Scope,depth);
    }

    public virtual string GenerateStruct(StructNode node,int depth)
    {
        return $"struct {node.Name}" + "{\n" + node.Declarations
                   .Select(nodDeclaration => Tabs(depth + 1) + GenerateDeclaration(nodDeclaration,depth) + ";\n")
                   .Aggregate("", (c, d) => c + d) +
               "};\n";
    }

    public virtual string GenerateAssign(AssignNode node)
    {
        return $"{GenerateExpression(node.Left)} = {GenerateExpression(node.Right)}";
    }
    
    public virtual string GenerateBinaryOpAndAssign(BinaryOpAndAssignNode node)
    {
        return GenerateExpression(node.Left) + node.Op switch
        {
            BinaryOp.Add => " += ",
            BinaryOp.Subtract => " -= ",
            BinaryOp.Divide => " /= ",
            BinaryOp.Multiply => " *= ",
            _ => throw new ArgumentOutOfRangeException()
        } + GenerateExpression(node.Right);
    }

    public virtual string GenerateTags(Dictionary<string, string> tags)
    {
        var isFirst = true;
        var result = "";
        
        foreach (var (tag,val) in tags)
        {
            if(tag.StartsWith('$')) continue;

            result += (isFirst ? "" : " , ") + tag;
            isFirst = false;
            if (val.Length != 0)
            {
                result += " = " + val;
            }
        }

        return result;
    }
    public string GenerateLayout(LayoutNode node,int depth)
    {
        var result = $"layout({GenerateTags(node.Tags)}) ";
        
        if (node.Tags.ContainsKey("$flat"))
        {
            result += "flat ";
        }
        
        return result + node.Qualifier switch
            {
                LayoutQualifier.In => "in",
                LayoutQualifier.Out => "out",
                LayoutQualifier.Uniform => "uniform",
                LayoutQualifier.ReadOnly => "readonly",
                _ => throw new ArgumentOutOfRangeException()
            } + $" {GenerateDeclaration(node.Declaration,depth)};\n";
    }
    
    public string GenerateDefine(DefineNode node)
    {
        return $"#define {node.Identifier} {GenerateExpression(node.Expression)}\n";
    }

    public virtual string GeneratePushConstant(PushConstantNode node,int depth)
    {
        var result = "layout(push_constant" + (node.Tags.Count == 0 ? ")" : " , " + GenerateTags(node.Tags)) + ")";
        return result + " uniform constant " + "{\n" + node.Declarations
                   .Select(nodDeclaration => Tabs(depth + 1) + GenerateDeclaration(nodDeclaration,depth) + ";\n")
                   .Aggregate("", (c, d) => c + d) +
               Tabs(depth) + "} push;\n";
    }
    
    public virtual string GenerateSSBO(SSBONode node,int depth)
    {
        return "layout(" + GenerateTags(node.Tags) + $") buffer _buffer_{node.Name}" + "{\n" + node.Declarations
                   .Select(nodDeclaration => Tabs(depth + 1) + GenerateDeclaration(nodDeclaration,depth) + ";\n")
                   .Aggregate("", (c, d) => c + d) +
               Tabs(depth) + "} " + node.Name + ";\n";
    }

    public override string Run(IEnumerable<Node> nodes)
    {
        var result = "";

        foreach (var node in nodes)
        {
            switch (node.NodeType)
            {
                case NodeType.Function:
                {
                    if (node is FunctionNode asFunction)
                    {
                        result += GenerateFunction(asFunction,0);
                        break;
                    }
                }
                    goto default;
                case NodeType.Layout:
                {
                    if (node is LayoutNode asLayout)
                    {
                        result += GenerateLayout(asLayout,0);
                        break;
                    }
                }
                    goto default;
                case NodeType.Define:
                {
                    if (node is DefineNode asDefine)
                    {
                        result += GenerateDefine(asDefine);
                        break;
                    }
                }
                    goto default;
                case NodeType.PushConstant:
                {
                    if (node is PushConstantNode asPushConstant)
                    {
                        result += GeneratePushConstant(asPushConstant,0);
                        break;
                    }
                }
                    goto default;
                case NodeType.SSBO:
                {
                    if (node is SSBONode asSSBO)
                    {
                        result += GenerateSSBO(asSSBO,0);
                        break;
                    }
                }
                    goto default;
                case NodeType.Struct:
                {
                    if (node is StructNode asStruct)
                    {
                        result += GenerateStruct(asStruct,0);
                        break;
                    }
                }
                    goto default;
                case NodeType.Assign:
                {
                    result += GenerateStatement(node,0);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            result += "\n";
        }

        return result;
    }
}