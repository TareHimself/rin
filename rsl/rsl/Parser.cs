using System.Collections.Frozen;
using rsl.Nodes;


namespace rsl;

public class Parser
{
    public static TokenList ConsumeTokensTill(ref TokenList input, HashSet<TokenType> targets, int initialScope = 0,
        bool includeTarget = false)
    {
        var result = new TokenList();
        var scope = initialScope;
        while (input.NotEmpty())
        {
            var frontToken = input.Front();

            switch (frontToken.Type)
            {
                case TokenType.OpenBrace or TokenType.OpenParen or TokenType.OpenBracket:
                    scope++;
                    break;
                case TokenType.CloseBrace or TokenType.CloseParen or TokenType.CloseBracket:
                    scope--;
                    break;
            }

            if (targets.Contains(frontToken.Type) && scope == 0)
            {
                if (includeTarget)
                {
                    result.InsertBack(input.RemoveFront());
                }

                return result;
            }

            result.InsertBack(input.RemoveFront());
        }

        return result;
    }

//     std::shared_ptr<Node> parseParen(TokenList& input);
//
//     std::shared_ptr<ArrayLiteralNode> parseArrayLiteral(TokenList& input);
//
//     std::shared_ptr<Node> parsePrimary(TokenList& input);
//
//     std::shared_ptr<Node> parseAccessors(TokenList& input, const std::shared_ptr<Node>& initialLeft = {});
//
// std::shared_ptr<Node> parseMultiplicativeExpression(TokenList& input);
//
// std::shared_ptr<Node> parseAdditiveExpression(TokenList& input);
//
// std::shared_ptr<Node> parseComparisonExpression(TokenList& input);
//
// std::shared_ptr<Node> parseLogicalExpression(TokenList& input);
//
// std::shared_ptr<Node> parseConditionalExpression(TokenList& input);
//
// std::shared_ptr<Node> parseAssignmentExpression(TokenList& input);

    public static Node ResolveTokenToLiteralOrIdentifier(Token token)
    {
        {
            if (int.TryParse(token.Value, out var result))
            {
                return new IntLiteral(result);
            }
        }

        {
            if (bool.TryParse(token.Value, out var result))
            {
                return new BooleanLiteral(result);
            }
        }

        {
            if (float.TryParse(token.Value, out var result))
            {
                return new FloatLiteral(result);
            }
        }

        return new IdentifierNode(token.Value);
    }

    public static Node ParseArrayLiteral(ref TokenList input)
    {
        input.ExpectFront(TokenType.OpenBrace).RemoveFront();

        var allItemsTokens = ConsumeTokensTill(ref input, [TokenType.CloseBrace], 1);

        input.ExpectBack(TokenType.CloseBrace).RemoveFront();

        List<Node> nodes = [];

        while (allItemsTokens.NotEmpty())
        {
            var itemTokens = ConsumeTokensTill(ref allItemsTokens, [TokenType.Comma]);

            if (allItemsTokens.NotEmpty())
            {
                allItemsTokens.ExpectFront(TokenType.Comma).RemoveFront();
            }

            nodes.Add(ParseExpression(ref itemTokens));
        }

        return new ArrayLiteralNode(nodes);
    }

    public static Node ParsePrimary(ref TokenList input)
    {
        switch (input.Front().Type)
        {
            case TokenType.Const:
            {
                input.RemoveFront();
                return new ConstNode(ParseDeclaration(ref input));
            }
            case TokenType.Numeric:
                return ResolveTokenToLiteralOrIdentifier(input.RemoveFront());
            case TokenType.Identifier or TokenType.Unknown:
            {
                var front = input.RemoveFront();
                if (input.NotEmpty() && input.Front().Type == TokenType.Unknown)
                {
                    input.InsertFront(front);
                    return ParseDeclaration(ref input);
                }

                input.InsertFront(front);
                goto case TokenType.Numeric;
            }
            case TokenType.TypeVoid or TokenType.TypeFloat or TokenType.TypeFloat2 or TokenType.TypeFloat3
                or TokenType.TypeFloat4
                or TokenType.TypeInt or TokenType.TypeInt2 or TokenType.TypeInt3 or TokenType.TypeInt4
                or TokenType.TypeMat3 or TokenType.TypeMat4:
            {
                var targetToken = input.RemoveFront();
                if (input.NotEmpty() && input.Front().Type is TokenType.Identifier or TokenType.Unknown)
                {
                    input.InsertFront(targetToken);
                    return ParseDeclaration(ref input);
                }

                return ParseAccessorsExpression(ref input, ResolveTokenToLiteralOrIdentifier(targetToken));
            }
            case TokenType.OpIncrement or TokenType.OpDecrement:
            {
                var op = input.RemoveFront();
                var next = ParseAccessorsExpression(ref input);
                if (op.Type == TokenType.OpIncrement)
                {
                    return new IncrementNode(true, next);
                }

                return new DecrementNode(true, next);
            }
            case TokenType.OpenParen:
            {
                var parenTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen]);

                parenTokens.RemoveFront();

                input.ExpectFront(TokenType.CloseParen).RemoveFront();

                return new PrecedenceNode(ParseExpression(ref parenTokens));
            }
            case TokenType.OpenBrace:
                return ParseArrayLiteral(ref input);
            case TokenType.OpSubtract:
            {
                input.RemoveFront();
                return new NegateNode(ParsePrimary(ref input));
            }
            case TokenType.PushConstant:
            {
                var tok = input.RemoveFront();
                return new IdentifierNode(tok.Value);
            }
            case TokenType.Discard:
                return new DiscardNode();
            default:
                throw new Exception("Unknown Primary Token");
        }
    }

    public static Node ParseIncrementDecrementExpression(ref TokenList input)
    {
        var left = ParsePrimary(ref input);
        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpIncrement or TokenType.OpDecrement)
        {
            var op = input.RemoveFront();
            if (op.Type == TokenType.OpIncrement)
            {
                return new IncrementNode(false, left);
            }
            else
            {
                left = new DecrementNode(false, left);
            }
        }

        return left;
    }

    public static Node ParseAccessorsExpression(ref TokenList input, Node? initialLeft = null)
    {
        var left = initialLeft ?? ParseIncrementDecrementExpression(ref input);
        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpenParen or TokenType.Access or TokenType.OpenBracket)
        {
            switch (input.Front().Type)
            {
                case TokenType.OpenParen:
                {
                    if (left is IdentifierNode identifier)
                    {
                        input.RemoveFront();
                        var allArgsTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);
                        input.ExpectFront(TokenType.CloseParen).RemoveFront();

                        List<Node> args = [];

                        while (allArgsTokens.NotEmpty())
                        {
                            var argsTokens = ConsumeTokensTill(ref allArgsTokens, [TokenType.Comma]);

                            if (allArgsTokens.NotEmpty())
                            {
                                allArgsTokens.ExpectFront(TokenType.Comma).RemoveFront();
                            }

                            args.Add(ParseExpression(ref argsTokens));
                        }

                        left = new CallNode(identifier, args);
                    }
                    else
                    {
                        return left;
                    }
                }
                    break;
                case TokenType.Access:
                {
                    var token = input.RemoveFront();
                    var right = ParseIncrementDecrementExpression(ref input);
                    left = new AccessNode(left, right);
                }
                    break;
                case TokenType.OpenBracket:
                {
                    var token = input.RemoveFront();
                    var exprTokens = ConsumeTokensTill(ref input, [TokenType.CloseBracket], 1);
                    input.ExpectFront(TokenType.CloseBracket).RemoveFront();
                    left = new IndexNode(left, ParseExpression(ref exprTokens));
                }
                    break;
            }
        }

        return left;
    }

    public static Node ParseMultiplicativeExpression(ref TokenList input)
    {
        var left = ParseAccessorsExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpDivide or TokenType.OpMultiply)
        {
            var token = input.RemoveFront();
            var right = ParseAccessorsExpression(ref input);
            left = new BinaryOpNode(left, right, token.Type);
        }

        return left;
    }

    public static Node ParseAdditiveExpression(ref TokenList input)
    {
        var left = ParseMultiplicativeExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpAdd or TokenType.OpSubtract)
        {
            var token = input.RemoveFront();
            var right = ParseMultiplicativeExpression(ref input);
            left = new BinaryOpNode(left, right, token.Type);
        }

        return left;
    }

    public static Node ParseComparisonExpression(ref TokenList input)
    {
        var left = ParseAdditiveExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpEqual or TokenType.OpNotEqual or TokenType.OpLess
                   or TokenType.OpLessEqual or TokenType.OpGreater or TokenType.OpGreaterEqual)
        {
            var token = input.RemoveFront();
            var right = ParseAdditiveExpression(ref input);
            left = new BinaryOpNode(left, right, token.Type);
        }

        return left;
    }

    public static Node ParseLogicalExpression(ref TokenList input)
    {
        var left = ParseComparisonExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpAnd or TokenType.OpNot or TokenType.OpOr)
        {
            var token = input.RemoveFront();
            var right = ParseComparisonExpression(ref input);
            left = new BinaryOpNode(left, right, token.Type);
        }

        return left;
    }

    public static Node ParseConditionalExpression(ref TokenList input)
    {
        var left = ParseLogicalExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.Conditional)
        {
            var token = input.RemoveFront();
            var leftTokens = ConsumeTokensTill(ref input, [TokenType.Colon]);
            input.ExpectFront(TokenType.Colon).RemoveFront();
            left = new ConditionalNode(left, ParseExpression(ref leftTokens), ParseExpression(ref input));
        }

        return left;
    }

    public static Node ParseAssignmentExpression(ref TokenList input)
    {
        var left = ParseConditionalExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.Assign or TokenType.OpAddAssign or TokenType.OpSubtractAssign
                   or TokenType.OpMultiplyAssign or TokenType.OpDivideAssign)
        {
            switch (input.Front().Type)
            {
                case TokenType.Assign:
                {
                    var token = input.RemoveFront();
                    var right = ParseConditionalExpression(ref input);
                    left = new AssignNode(left, right);
                }
                    break;
                default:
                {
                    var token = input.RemoveFront();
                    var right = ParseConditionalExpression(ref input);
                    left = new BinaryOpAndAssignNode(left, right, token.Type switch
                    {
                        TokenType.OpAddAssign => BinaryOp.Add,
                        TokenType.OpSubtractAssign => BinaryOp.Subtract,
                        TokenType.OpMultiplyAssign => BinaryOp.Multiply,
                        TokenType.OpDivideAssign => BinaryOp.Divide,
                        _ => throw new ArgumentOutOfRangeException()
                    });
                }
                    break;
            }
        }

        return left;
    }

    public static Node ParseExpression(ref TokenList input)
    {
        return ParseAssignmentExpression(ref input);
    }

    public static NamedScopeNode ParseNamedScope(ref TokenList input)
    {
        List<Node> statements = [];

        var scopeTypeToken = input.RemoveFront();

        input.ExpectFront(TokenType.OpenBrace).RemoveFront();

        while (input.Front().Type != TokenType.CloseBrace)
        {
            switch (input.Front().Type)
            {
                case TokenType.Include:
                    statements.Add(ParseInclude(ref input));
                    break;
                case TokenType.Define:
                    statements.Add(ParseDefine(ref input));
                    break;
                case TokenType.Layout:
                    statements.Add(ParseLayout(ref input));
                    break;
                case TokenType.TypeStruct:
                    statements.Add(ParseStruct(ref input));
                    break;
                case TokenType.Const:
                {
                    var tokens = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
                    input.ExpectFront(TokenType.StatementEnd).RemoveFront();
                    statements.Add(ParseExpression(ref tokens));
                }
                    break;
                case TokenType.PushConstant:
                {
                    statements.Add(ParsePushConstant(ref input));
                }
                    break;
                case TokenType.Compute:
                {
                    statements.Add(ParseCompute(ref input));
                }
                    break;
                case TokenType.SSBO:
                    statements.Add(ParseSSBO(ref input));
                    break;
                case TokenType.TypeVoid or TokenType.TypeFloat or TokenType.TypeFloat2 or TokenType.TypeFloat3
                    or TokenType.TypeFloat4
                    or TokenType.TypeInt or TokenType.TypeInt2 or TokenType.TypeInt3 or TokenType.TypeInt4
                    or TokenType.TypeBoolean or TokenType.TypeMat3 or TokenType.TypeMat4 or TokenType.Unknown:
                    statements.Add(ParseFunction(ref input));
                    break;
                default:
                {
                    var statementTokens = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);

                    input.ExpectFront(TokenType.StatementEnd).RemoveFront();

                    statements.Add(ParseExpression(ref statementTokens));
                }
                    break;
            }
        }

        input.RemoveFront();

        ScopeType scopeType = scopeTypeToken.Type switch
        {
            TokenType.FragmentScope => ScopeType.Fragment,
            TokenType.VertexScope => ScopeType.Vertex,
            TokenType.ComputeScope => ScopeType.Compute,
            _ => ScopeType.Vertex
        };

        return new NamedScopeNode(scopeType, statements);
    }

    public static IncludeNode ParseInclude(ref TokenList input)
    {
        input.ExpectFront(TokenType.Include).RemoveFront();
        var token = input.ExpectFront(TokenType.String).RemoveFront();
        return new IncludeNode(token.DebugInfo.File, token.Value);
    }

    public static DefineNode ParseDefine(ref TokenList input)
    {
        input.ExpectFront(TokenType.Define).RemoveFront();
        var identifier = input.RemoveFront();
        var expr = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
        input.ExpectFront(TokenType.StatementEnd).RemoveFront();
        return new DefineNode(identifier.Value, ParseExpression(ref expr));
    }

    public static ComputeNode ParseCompute(ref TokenList input)
    {
        input.ExpectFront(TokenType.Compute).RemoveFront();

        input.ExpectFront(TokenType.OpenParen).RemoveFront();

        var tagTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);

        input.ExpectFront(TokenType.CloseParen).RemoveFront();
        Dictionary<string, string> tags = [];
        while (tagTokens.NotEmpty())
        {
            var id = tagTokens.RemoveFront();
            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Assign)
            {
                tagTokens.RemoveFront();
                var val = tagTokens.RemoveFront();
                tags.Add(id.Value, val.Value);
            }
            else
            {
                tags.Add(id.Value, "");
            }

            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Comma)
            {
                tagTokens.RemoveFront();
            }
        }

        input.ExpectFront(TokenType.StatementEnd).RemoveFront();

        return new ComputeNode(tags);
    }

    public static LayoutNode ParseLayout(ref TokenList input)
    {
        input.ExpectFront(TokenType.Layout).RemoveFront();

        input.ExpectFront(TokenType.OpenParen).RemoveFront();

        var tagTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);

        input.ExpectFront(TokenType.CloseParen).RemoveFront();
        Dictionary<string, string> tags = [];
        while (tagTokens.NotEmpty())
        {
            var id = tagTokens.RemoveFront();
            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Assign)
            {
                tagTokens.RemoveFront();
                var val = tagTokens.RemoveFront();
                tags.Add(id.Value, val.Value);
            }
            else
            {
                tags.Add(id.Value, "");
            }

            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Comma)
            {
                tagTokens.RemoveFront();
            }
        }

        var layoutTypeToken = input.RemoveFront();

        LayoutQualifier layoutQualifier = layoutTypeToken.Type switch
        {
            TokenType.DataIn => LayoutQualifier.In,
            TokenType.DataOut => LayoutQualifier.Out,
            TokenType.ReadOnly => LayoutQualifier.ReadOnly,
            TokenType.Uniform => LayoutQualifier.Uniform,
            _ => throw new ExceptionWithDebug(layoutTypeToken.DebugInfo, "Unknown layout qualifier")
        };

        var declaration = ParseDeclaration(ref input);

        input.ExpectFront(TokenType.StatementEnd).RemoveFront();

        return new LayoutNode(layoutQualifier, declaration, tags);
    }

    public static StructNode ParseStruct(ref TokenList input)
    {
        input.ExpectFront(TokenType.TypeStruct).RemoveFront();
        var name = input.RemoveFront();
        var declarations = ParseStructScope(ref input);
        input.ExpectFront(TokenType.StatementEnd).RemoveFront();
        return new StructNode(name.Value, declarations);
    }

    public static DeclarationNode[] ParseStructScope(ref TokenList input)
    {
        List<DeclarationNode> result = [];
        input.ExpectFront(TokenType.OpenBrace).RemoveFront();
        while (input.Front().Type != TokenType.CloseBrace)
        {
            var declarationTokens = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
            input.ExpectFront(TokenType.StatementEnd).RemoveFront();
            result.Add(ParseDeclaration(ref declarationTokens));
        }

        input.RemoveFront();

        return result.ToArray();
    }

    public static PushConstantNode ParsePushConstant(ref TokenList input)
    {
        input.ExpectFront(TokenType.PushConstant).RemoveFront();

        input.ExpectFront(TokenType.OpenParen).RemoveFront();

        var tagTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);

        input.ExpectFront(TokenType.CloseParen).RemoveFront();
        Dictionary<string, string> tags = [];
        while (tagTokens.NotEmpty())
        {
            var id = tagTokens.RemoveFront();
            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Assign)
            {
                tagTokens.RemoveFront();
                var val = tagTokens.RemoveFront();
                tags.Add(id.Value, val.Value);
            }
            else
            {
                tags.Add(id.Value, "");
            }

            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Comma)
            {
                tagTokens.RemoveFront();
            }
        }

        var declarations = ParseStructScope(ref input);

        input.ExpectFront(TokenType.StatementEnd).RemoveFront();

        return new PushConstantNode(declarations, tags);
    }

    public static SSBONode ParseSSBO(ref TokenList input)
    {
        input.ExpectFront(TokenType.SSBO).RemoveFront();

        input.ExpectFront(TokenType.OpenParen).RemoveFront();

        var tagTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);

        input.ExpectFront(TokenType.CloseParen).RemoveFront();
        Dictionary<string, string> tags = [];
        while (tagTokens.NotEmpty())
        {
            var id = tagTokens.RemoveFront();
            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Assign)
            {
                tagTokens.RemoveFront();
                var val = tagTokens.RemoveFront();
                tags.Add(id.Value, val.Value);
            }
            else
            {
                tags.Add(id.Value, "");
            }

            if (tagTokens.NotEmpty() && tagTokens.Front().Type == TokenType.Comma)
            {
                tagTokens.RemoveFront();
            }
        }

        var name = input.ExpectFront(TokenType.Unknown).RemoveFront().Value;

        var declarations = ParseStructScope(ref input);

        input.ExpectFront(TokenType.StatementEnd).RemoveFront();

        return new SSBONode(name, declarations, tags);
    }

    public static IfNode ParseIf(ref TokenList input)
    {
        input.ExpectFront(TokenType.If).RemoveFront();
        var condition = ConsumeTokensTill(ref input, [TokenType.CloseParen]);
        condition.ExpectFront(TokenType.OpenParen).RemoveFront();

        input.ExpectFront(TokenType.CloseParen).RemoveFront();

        var cond = ParseExpression(ref condition);

        var scope = ParseScope(ref input);

        if (input.NotEmpty() && input.Front().Type == TokenType.Else)
        {
            input.RemoveFront();
            Node elseScope = input.Front().Type == TokenType.If ? ParseIf(ref input) : ParseScope(ref input);

            return new IfNode(cond, scope, elseScope);
        }

        return new IfNode(cond, scope);
    }

    public static ForNode ParseFor(ref TokenList input)
    {
        input.ExpectFront(TokenType.For).RemoveFront();

        var withinParen = ConsumeTokensTill(ref input, [TokenType.CloseParen]);

        withinParen.ExpectFront(TokenType.OpenParen).RemoveFront();

        input.ExpectFront(TokenType.CloseParen).RemoveFront();

        var initTokens = ConsumeTokensTill(ref withinParen, [TokenType.StatementEnd]);

        withinParen.ExpectFront(TokenType.StatementEnd).RemoveFront();

        var condTokens = ConsumeTokensTill(ref withinParen, [TokenType.StatementEnd]);

        withinParen.ExpectFront(TokenType.StatementEnd).RemoveFront();

        var noop = new NoOpNode();

        return new ForNode(initTokens.Empty() ? noop : ParseExpression(ref initTokens),
            condTokens.Empty() ? noop : ParseExpression(ref condTokens),
            withinParen.Empty() ? noop : ParseExpression(ref withinParen), ParseScope(ref input));
    }

    public static ScopeNode ParseScope(ref TokenList input)
    {
        List<Node> statements = [];

        input.ExpectFront(TokenType.OpenBrace).RemoveFront();

        while (input.Front().Type != TokenType.CloseBrace)
        {
            switch (input.Front().Type)
            {
                case TokenType.If:
                    statements.Add(ParseIf(ref input));
                    break;
                case TokenType.For:
                    statements.Add(ParseFor(ref input));
                    break;
                case TokenType.Return:
                {
                    input.RemoveFront();
                    var statementTokens = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
                    input.ExpectFront(TokenType.StatementEnd).RemoveFront();

                    statements.Add(new ReturnNode(ParseExpression(ref statementTokens)));
                }
                    break;
                default:
                {
                    var statementTokens = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);

                    input.ExpectFront(TokenType.StatementEnd).RemoveFront();

                    statements.Add(ParseExpression(ref statementTokens));
                }
                    break;
            }
        }

        input.RemoveFront();

        return new ScopeNode(statements);
    }

    public static DeclarationNode ParseDeclaration(ref TokenList input)
    {
        var type = input.RemoveFront();

        if (type.Type == TokenType.TypeBuffer)
        {
            var name = input.ExpectFront(TokenType.Unknown).RemoveFront();
            var declarations = ParseStructScope(ref input);
            return new BufferDeclarationNode(name.Value, 1, declarations);
        }

        if (type.Type == TokenType.Unknown && input.NotEmpty() && input.Front().Type == TokenType.OpenBrace)
        {
            var name = type;
            var declarations = ParseStructScope(ref input);
            return new BlockDeclarationNode(name.Value, 1, declarations);
        }

        {
            var name = input.Front().Type == TokenType.Unknown
                ? input.ExpectFront(TokenType.Unknown).RemoveFront().Value
                : "";

            var returnCount = 1;

            if (input.NotEmpty() && input.Front().Type == TokenType.OpenBracket)
            {
                input.RemoveFront();
                returnCount = input.Front().Type == TokenType.Numeric ? int.Parse(input.RemoveFront().Value) : -1;
                input.ExpectFront(TokenType.CloseBracket).RemoveFront();
            }

            return type.Type == TokenType.Unknown
                ? new StructDeclarationNode(type.Value, name, returnCount)
                : new DeclarationNode(type.Type, name, returnCount);
        }
    }

    public static FunctionArgumentNode ParseFunctionArgument(ref TokenList input)
    {
        var isInput = true;

        if (input.Front().Type is TokenType.DataIn or TokenType.DataOut)
        {
            isInput = input.RemoveFront().Type == TokenType.DataIn;
        }

        var type = input.RemoveFront();

        var returnCount = 1;

        if (input.NotEmpty() && input.Front().Type == TokenType.OpenBracket)
        {
            input.RemoveFront();
            returnCount = input.Front().Type == TokenType.Numeric ? int.Parse(input.RemoveFront().Value) : -1;
            input.ExpectFront(TokenType.CloseBracket).RemoveFront();
        }

        var name = input.RemoveFront().Value;

        return new FunctionArgumentNode(isInput, new DeclarationNode(type.Type, name, returnCount));
    }

    public static FunctionNode ParseFunction(ref TokenList input)
    {
        var type = input.RemoveFront();
        var returnCount = 1;

        if (input.Front().Type == TokenType.OpenBracket)
        {
            input.RemoveFront();
            returnCount = input.Front().Type == TokenType.Numeric ? int.Parse(input.RemoveFront().Value) : -1;
            input.ExpectFront(TokenType.CloseBracket).RemoveFront();
        }

        var name = input.ExpectFront(TokenType.Unknown).RemoveFront();

        input.ExpectFront(TokenType.OpenParen).RemoveFront();
        var allArgsTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);
        input.ExpectFront(TokenType.CloseParen).RemoveFront();

        List<FunctionArgumentNode> args = [];

        while (allArgsTokens.NotEmpty())
        {
            var argsTokens = ConsumeTokensTill(ref allArgsTokens, [TokenType.Comma]);

            if (allArgsTokens.NotEmpty())
            {
                allArgsTokens.ExpectFront(TokenType.Comma).RemoveFront();
            }

            args.Add(ParseFunctionArgument(ref argsTokens));
        }

        var returnDecl = type.Type == TokenType.Unknown
            ? new StructDeclarationNode(type.Value, "", returnCount)
            : new DeclarationNode(type.Type, "", returnCount);

        if (input.Front().Type == TokenType.Arrow)
        {
            input.RemoveFront();
            var expr = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
            input.ExpectFront(TokenType.StatementEnd).RemoveFront();
            return new FunctionNode(returnDecl, name.Value, args,
                new ScopeNode([new ReturnNode(ParseExpression(ref expr))]));
        }

        return new FunctionNode(returnDecl, name.Value, args, ParseScope(ref input));
    }

    public static ModuleNode Parse(ref TokenList input)
    {
        if (input.Empty()) return new ModuleNode("", []);
        var file = input.Front().DebugInfo.File;
        List<Node> statements = [];
        while (input.NotEmpty())
        {
            switch (input.Front().Type)
            {
                case TokenType.FragmentScope or TokenType.VertexScope or TokenType.ComputeScope:
                    statements.Add(ParseNamedScope(ref input));
                    break;
                case TokenType.Include:
                    statements.Add(ParseInclude(ref input));
                    break;
                case TokenType.Define:
                    statements.Add(ParseDefine(ref input));
                    break;
                case TokenType.Layout:
                    statements.Add(ParseLayout(ref input));
                    break;
                case TokenType.TypeStruct:
                    statements.Add(ParseStruct(ref input));
                    break;
                case TokenType.Const:
                {
                    var tokens = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
                    input.ExpectFront(TokenType.StatementEnd).RemoveFront();
                    statements.Add(ParseExpression(ref tokens));
                }
                    break;
                case TokenType.PushConstant:
                    statements.Add(ParsePushConstant(ref input));
                    break;
                case TokenType.SSBO:
                    statements.Add(ParseSSBO(ref input));
                    break;
                case TokenType.TypeVoid or TokenType.TypeFloat or TokenType.TypeFloat2 or TokenType.TypeFloat3
                    or TokenType.TypeFloat4
                    or TokenType.TypeInt or TokenType.TypeInt2 or TokenType.TypeInt3 or TokenType.TypeInt4
                    or TokenType.TypeBoolean or TokenType.TypeMat3 or TokenType.TypeMat4 or TokenType.Unknown:
                    statements.Add(ParseFunction(ref input));
                    break;
                default:
                    throw new ExceptionWithDebug(input.Front().DebugInfo, "Unknown node type");
            }
        }

        return new ModuleNode(file, statements);
    }
}