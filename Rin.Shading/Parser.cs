using Rin.Shading.Ast.Nodes;
using Rin.Shading.Exceptions;

namespace Rin.Shading;

public static class Parser
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
                if (includeTarget) result.InsertBack(input.RemoveFront());

                return result;
            }

            result.InsertBack(input.RemoveFront());
        }

        return result;
    }

//     std::shared_ptr<INode> parseParen(TokenList& input);
//
//     std::shared_ptr<ArrayLiteralNode> parseArrayLiteral(TokenList& input);
//
//     std::shared_ptr<INode> parsePrimary(TokenList& input);
//
//     std::shared_ptr<INode> parseAccessors(TokenList& input, const std::shared_ptr<INode>& initialLeft = {});
//
// std::shared_ptr<INode> parseMultiplicativeExpression(TokenList& input);
//
// std::shared_ptr<INode> parseAdditiveExpression(TokenList& input);
//
// std::shared_ptr<INode> parseComparisonExpression(TokenList& input);
//
// std::shared_ptr<INode> parseLogicalExpression(TokenList& input);
//
// std::shared_ptr<INode> parseConditionalExpression(TokenList& input);
//
// std::shared_ptr<INode> parseAssignmentExpression(TokenList& input);

    public static INode ResolveTokenToLiteralOrIdentifier(Token token)
    {
        {
            if (int.TryParse(token.Value, out var result))
                return new IntLiteralNode
                {
                    Value = result
                };
        }

        {
            if (bool.TryParse(token.Value, out var result))
                return new BooleanLiteralNode
                {
                    Value = result
                };
        }

        {
            if (float.TryParse(token.Value, out var result))
                return new FloatLiteralNode
                {
                    Value = token.Value
                };
        }

        return new IdentifierNode
        {
            Value = token.Value
        };
    }

    public static ArrayLiteralNode ParseArrayLiteral(ref TokenList input)
    {
        input.ExpectFront(TokenType.OpenBrace).RemoveFront();

        var allItemsTokens = ConsumeTokensTill(ref input, [TokenType.CloseBrace], 1);

        input.ExpectBack(TokenType.CloseBrace).RemoveFront();

        List<INode> nodes = [];

        while (allItemsTokens.NotEmpty())
        {
            var itemTokens = ConsumeTokensTill(ref allItemsTokens, [TokenType.Comma]);

            if (allItemsTokens.NotEmpty()) allItemsTokens.ExpectFront(TokenType.Comma).RemoveFront();

            nodes.Add(ParseExpression(ref itemTokens));
        }

        return new ArrayLiteralNode
        {
            Items = nodes.ToArray()
        };
    }

    public static INode ParsePrimary(ref TokenList input)
    {
        switch (input.Front().Type)
        {
            case TokenType.Const:
            {
                input.RemoveFront();
                return ParseVariableDeclaration(ref input);
            }
            case TokenType.OpNot:
            {
                input.RemoveFront();
                return new NotNode
                {
                    Expression = ParsePrimary(ref input)
                };
            }
            case TokenType.Numeric:
                return ResolveTokenToLiteralOrIdentifier(input.RemoveFront());
            case TokenType.Identifier or TokenType.Unknown:
            {
                var front = input.RemoveFront();
                if (input.NotEmpty() && input.Front().Type == TokenType.Unknown)
                {
                    input.InsertFront(front);
                    return ParseVariableDeclaration(ref input);
                }

                input.InsertFront(front);
                goto case TokenType.Numeric;
            }
            case TokenType.OpIncrement or TokenType.OpDecrement:
            {
                var op = input.RemoveFront();
                var next = ParseAccessorsExpression(ref input);
                if (op.Type == TokenType.OpIncrement)
                    return new IncrementNode
                    {
                        Expression = next,
                        Before = true
                    };

                return new DecrementNode
                {
                    Expression = next,
                    Before = true
                };
            }
            case TokenType.OpenParen:
            {
                var parenTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen]);

                parenTokens.RemoveFront();

                input.ExpectFront(TokenType.CloseParen).RemoveFront();

                return new PrecedenceNode
                {
                    Expression = ParseExpression(ref parenTokens)
                };
            }
            case TokenType.OpenBrace:
                return ParseArrayLiteral(ref input);
            case TokenType.OpSubtract:
            {
                input.RemoveFront();
                return new NegateNode
                {
                    Expression = ParsePrimary(ref input)
                };
            }
            case TokenType.PushConstant:
            {
                var tok = input.RemoveFront();
                return new IdentifierNode
                {
                    Value = tok.Value
                };
            }
            case TokenType.Discard:
                return new DiscardNode();
            default:
                if (!BuiltInTypeNode.IsBuiltInType(input.Front().Type)) throw new Exception("Unknown Primary Token");

                var targetToken = input.RemoveFront();
                if (input.NotEmpty() && input.Front().Type is TokenType.Identifier or TokenType.Unknown)
                {
                    input.InsertFront(targetToken);
                    return ParseVariableDeclaration(ref input);
                }

                return ParseAccessorsExpression(ref input, ResolveTokenToLiteralOrIdentifier(targetToken));
        }
    }

    public static INode ParseAccessorsExpression(ref TokenList input, INode? initialLeft = null)
    {
        var left = initialLeft ?? ParsePrimary(ref input);
        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpenParen or TokenType.Access or TokenType.Arrow
                   or TokenType.OpenBracket)
            switch (input.Front().Type)
            {
                case TokenType.OpenParen:
                {
                    if (left is IdentifierNode or AccessNode or PointerAccessNode)
                    {
                        input.RemoveFront();
                        var allArgsTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);
                        input.ExpectFront(TokenType.CloseParen).RemoveFront();

                        List<INode> args = [];

                        while (allArgsTokens.NotEmpty())
                        {
                            var argsTokens = ConsumeTokensTill(ref allArgsTokens, [TokenType.Comma]);

                            if (allArgsTokens.NotEmpty()) allArgsTokens.ExpectFront(TokenType.Comma).RemoveFront();

                            args.Add(ParseExpression(ref argsTokens));
                        }

                        left = new CallNode
                        {
                            Target = left,
                            Arguments = args.ToArray()
                        };
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
                    var id = input.ExpectFront(TokenType.Unknown).RemoveFront();
                    //var right = ParsePrimary(ref input);
                    left = new AccessNode
                    {
                        Target = left,
                        Identifier = new IdentifierNode
                        {
                            Value = id.Value
                        }
                    };
                }
                    break;
                case TokenType.Arrow:
                {
                    var token = input.RemoveFront();
                    var id = input.ExpectFront(TokenType.Unknown).RemoveFront();
                    //var right = ParsePrimary(ref input);
                    left = new PointerAccessNode
                    {
                        Target = left,
                        Identifier = new IdentifierNode
                        {
                            Value = id.Value
                        }
                    };
                }
                    break;
                case TokenType.OpenBracket:
                {
                    var token = input.RemoveFront();
                    var exprTokens = ConsumeTokensTill(ref input, [TokenType.CloseBracket], 1);
                    input.ExpectFront(TokenType.CloseBracket).RemoveFront();
                    left = new IndexNode
                    {
                        Target = left,
                        Expression = ParseExpression(ref exprTokens)
                    };
                }
                    break;
            }

        return left;
    }

    public static INode ParseMultiplicativeExpression(ref TokenList input)
    {
        var left = ParseAccessorsExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpDivide or TokenType.OpMultiply)
        {
            var token = input.RemoveFront();
            var right = ParseAccessorsExpression(ref input);
            left = new BinaryOperatorNode
            {
                Left = left,
                Right = right,
                Operator = token.Type switch
                {
                    TokenType.OpDivide => BinaryOperator.Division,
                    TokenType.OpMultiply => BinaryOperator.Multiplication,
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        return left;
    }

    public static INode ParseAdditiveExpression(ref TokenList input)
    {
        var left = ParseMultiplicativeExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpAdd or TokenType.OpSubtract)
        {
            var token = input.RemoveFront();
            var right = ParseMultiplicativeExpression(ref input);
            left = new BinaryOperatorNode
            {
                Left = left,
                Right = right,
                Operator = token.Type switch
                {
                    TokenType.OpAdd => BinaryOperator.Addition,
                    TokenType.OpSubtract => BinaryOperator.Subtraction,
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        return left;
    }

    public static INode ParseComparisonExpression(ref TokenList input)
    {
        var left = ParseAdditiveExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpEqual or TokenType.OpNotEqual or TokenType.OpLess
                   or TokenType.OpLessEqual or TokenType.OpGreater or TokenType.OpGreaterEqual)
        {
            var token = input.RemoveFront();
            var right = ParseAdditiveExpression(ref input);
            left = new BinaryOperatorNode
            {
                Left = left,
                Right = right,
                Operator = token.Type switch
                {
                    TokenType.OpEqual => BinaryOperator.Equal,
                    TokenType.OpNotEqual => BinaryOperator.NotEqual,
                    TokenType.OpLess => BinaryOperator.Less,
                    TokenType.OpLessEqual => BinaryOperator.LessEqual,
                    TokenType.OpGreater => BinaryOperator.Greater,
                    TokenType.OpGreaterEqual => BinaryOperator.GreaterEqual,
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        return left;
    }

    public static INode ParseLogicalExpression(ref TokenList input)
    {
        var left = ParseComparisonExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.OpAnd or TokenType.OpOr)
        {
            var token = input.RemoveFront();
            var right = ParseComparisonExpression(ref input);
            left = new BinaryOperatorNode
            {
                Left = left,
                Right = right,
                Operator = token.Type switch
                {
                    TokenType.OpAnd => BinaryOperator.And,
                    TokenType.OpOr => BinaryOperator.Or,
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        return left;
    }

    public static INode ParseConditionalExpression(ref TokenList input)
    {
        var left = ParseLogicalExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.Conditional)
        {
            var token = input.RemoveFront();
            var leftTokens = ConsumeTokensTill(ref input, [TokenType.Colon]);
            input.ExpectFront(TokenType.Colon).RemoveFront();
            left = new ConditionalNode
            {
                Condition = ParseExpression(ref input),
                Left = left,
                Right = ParseExpression(ref leftTokens)
            };
        }

        return left;
    }

    public static INode ParseAsExpression(ref TokenList input)
    {
        var left = ParseConditionalExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.As)
        {
            input.RemoveFront();
            var right = ParseType(ref input);
            left = new AsNode
            {
                Target = left,
                Type = right
            };
        }

        return left;
    }

    public static INode ParseAssignmentExpression(ref TokenList input)
    {
        var left = ParseAsExpression(ref input);

        while (input.NotEmpty() &&
               input.Front().Type is TokenType.Assign)
        {
            input.RemoveFront();
            var right = ParseAsExpression(ref input);
            left = new BinaryOperatorNode
            {
                Left = left,
                Right = right,
                Operator = BinaryOperator.Assign
            };
        }

        return left;
    }

    public static INode ParseExpression(ref TokenList input)
    {
        return ParseAssignmentExpression(ref input);
    }

    public static NamedScopeNode ParseNamedScope(ref TokenList input)
    {
        input.ExpectFront(TokenType.NamedScopeBegin).RemoveFront();

        Dictionary<string, string> tags = [];

        // Optionally parse tags
        if (input.Front().Type == TokenType.OpenParen)
        {
            input.ExpectFront(TokenType.OpenParen).RemoveFront();
            var allArgsTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);
            input.ExpectFront(TokenType.CloseParen).RemoveFront();


            while (allArgsTokens.NotEmpty())
            {
                var argsTokens = ConsumeTokensTill(ref allArgsTokens, [TokenType.Comma]);

                if (allArgsTokens.NotEmpty()) allArgsTokens.ExpectFront(TokenType.Comma).RemoveFront();

                var id = argsTokens.RemoveFront().Value;

                if (argsTokens.NotEmpty() && allArgsTokens.Front().Type == TokenType.Assign) argsTokens.RemoveFront();

                tags.Add(id, argsTokens.NotEmpty() ? argsTokens.RemoveFront().Value : string.Empty);
            }
        }

        var name = input.RemoveFront().Value;
        //input.ExpectFront(TokenType.OpenBrace).RemoveFront();
        var scopeTokens = ConsumeTokensTill(ref input, [TokenType.CloseBrace]);
        scopeTokens.ExpectFront(TokenType.OpenBrace).RemoveFront();
        var statements = ParseGlobalScope(ref scopeTokens);
        input.ExpectFront(TokenType.CloseBrace).RemoveFront();
        return new NamedScopeNode
        {
            Name = name,
            Statements = statements,
            Tags = tags
        };
    }

    public static IncludeNode ParseInclude(ref TokenList input)
    {
        input.ExpectFront(TokenType.Include).RemoveFront();
        var token = input.ExpectFront(TokenType.String).RemoveFront();
        input.ExpectFront(TokenType.StatementEnd).RemoveFront();
        return new IncludeNode
        {
            Path = token.Value
        };
    }

    public static DefineNode ParseDefine(ref TokenList input)
    {
        input.ExpectFront(TokenType.Define).RemoveFront();
        var identifier = input.RemoveFront();
        var expr = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
        input.ExpectFront(TokenType.StatementEnd).RemoveFront();
        return new DefineNode
        {
            Name = identifier.Value,
            Expression = ParseExpression(ref expr)
        };
    }

    public static StructNode ParseStruct(ref TokenList input)
    {
        input.ExpectFront(TokenType.TypeStruct).RemoveFront();
        var name = input.RemoveFront().Value;
        List<StructVariableDeclarationNode> declarations = [];
        List<FunctionNode> functions = [];
        ParseStructScope(ref input, ref declarations, ref functions);
        return new StructNode
        {
            Name = name,
            Declarations = declarations.ToArray(),
            Functions = functions.ToArray()
        };
    }

    public static void ParseStructScope(ref TokenList input, ref List<StructVariableDeclarationNode> declarations,
        ref List<FunctionNode> functions)
    {
        input.ExpectFront(TokenType.OpenBrace).RemoveFront();
        while (input.Front().Type != TokenType.CloseBrace)
            if (input.Front().Type == TokenType.Function)
            {
                functions.Add(ParseFunction(ref input));
            }
            else
            {
                declarations.Add(ParseStructDeclaration(ref input));
                input.ExpectFront(TokenType.StatementEnd).RemoveFront();
            }

        input.ExpectFront(TokenType.CloseBrace).RemoveFront();
    }

    public static void ParsePushConstantScope(ref TokenList input, ref List<VariableDeclarationNode> declarations)
    {
        input.ExpectFront(TokenType.OpenBrace).RemoveFront();
        while (input.Front().Type != TokenType.CloseBrace)
        {
            declarations.Add(ParseVariableDeclaration(ref input));
            input.ExpectFront(TokenType.StatementEnd).RemoveFront();
        }

        input.ExpectFront(TokenType.CloseBrace).RemoveFront();
    }

    public static PushConstantNode ParsePushConstant(ref TokenList input)
    {
        input.ExpectFront(TokenType.PushConstant).RemoveFront();

        List<VariableDeclarationNode> declarations = [];

        ParsePushConstantScope(ref input, ref declarations);

        return new PushConstantNode
        {
            Declarations = declarations.ToArray()
        };
    }

    public static UniformNode ParseUniform(ref TokenList input)
    {
        input.ExpectFront(TokenType.Uniform).RemoveFront();

        input.ExpectFront(TokenType.OpenParen).RemoveFront();
        var allArgsTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);
        input.ExpectFront(TokenType.CloseParen).RemoveFront();

        Dictionary<string, string> tags = [];
        uint set = 0;
        uint binding = 0;
        while (allArgsTokens.NotEmpty())
        {
            var argsTokens = ConsumeTokensTill(ref allArgsTokens, [TokenType.Comma]);

            if (allArgsTokens.NotEmpty()) allArgsTokens.ExpectFront(TokenType.Comma).RemoveFront();

            var id = argsTokens.RemoveFront().Value;
            if (id is "set" or "binding")
            {
                argsTokens.ExpectFront(TokenType.Assign).RemoveFront();
                var next = argsTokens.RemoveFront().Value;
                switch (id)
                {
                    case "set":
                        set = uint.Parse(next);
                        break;
                    case "binding":
                        binding = uint.Parse(next);
                        break;
                }
            }
            else
            {
                if (argsTokens.NotEmpty() && allArgsTokens.Front().Type == TokenType.Assign) argsTokens.RemoveFront();

                tags.Add(id, argsTokens.NotEmpty() ? argsTokens.RemoveFront().Value : string.Empty);
            }
        }

        var declaration = ParseVariableDeclaration(ref input);

        input.ExpectFront(TokenType.StatementEnd).RemoveFront();

        return new UniformNode
        {
            Set = set,
            Binding = binding,
            Tags = tags,
            Declaration = declaration
        };
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
            INode elseNode = input.Front().Type == TokenType.If ? ParseIf(ref input) : ParseScope(ref input);

            return new IfNode
            {
                Condition = cond,
                Scope = scope,
                Else = elseNode
            };
        }

        return new IfNode
        {
            Condition = cond,
            Scope = scope
        };
    }

    public static ForNode ParseFor(ref TokenList input)
    {
        input.ExpectFront(TokenType.For).RemoveFront();

        var withinParen = ConsumeTokensTill(ref input, [TokenType.CloseParen]);

        withinParen.ExpectFront(TokenType.OpenParen).RemoveFront();

        input.ExpectFront(TokenType.CloseParen).RemoveBack();

        var initTokens = ConsumeTokensTill(ref withinParen, [TokenType.Colon]);

        withinParen.ExpectFront(TokenType.Colon).RemoveFront();

        var condTokens = ConsumeTokensTill(ref withinParen, [TokenType.Colon]);

        withinParen.ExpectFront(TokenType.Colon).RemoveFront();

        var noop = new NoOpNode();

        return new ForNode
        {
            Init = initTokens.Empty() ? noop : ParseExpression(ref initTokens),
            Condition = condTokens.Empty() ? noop : ParseExpression(ref condTokens),
            Update = withinParen.Empty() ? noop : ParseExpression(ref withinParen),
            Scope = ParseScope(ref input)
        };
    }

    public static INode[] ParseMultipleDeclarationLine(ref TokenList input)
    {
        List<INode> nodes = [];
        ParseVariableModifiers(ref input, out var isConstant, out var isStatic);
        var type = ParseType(ref input);
        do
        {
            var name = ParseVariableName(ref input);
            var count = ParseCount(ref input);
            if (input.Front().Type == TokenType.Assign)
            {
                input.RemoveFront();
                var expressionTokens = ConsumeTokensTill(ref input, [TokenType.Comma, TokenType.StatementEnd]);
                var expr = ParseExpression(ref expressionTokens);
                nodes.Add(new BinaryOperatorNode
                {
                    Operator = BinaryOperator.Assign,
                    Left = new VariableDeclarationNode
                    {
                        IsConstant = isConstant,
                        IsStatic = isStatic,
                        Type = type,
                        Name = name,
                        Count = count
                    },
                    Right = expr
                });
                if (input.Front().Type == TokenType.Comma) input.RemoveFront();
            }
            else
            {
                nodes.Add(new VariableDeclarationNode
                {
                    IsConstant = isConstant,
                    IsStatic = isStatic,
                    Type = type,
                    Name = name,
                    Count = count
                });
            }
        } while (input.NotEmpty() && input.Front().Type != TokenType.StatementEnd);

        return nodes.ToArray();
    }

    public static ScopeNode ParseScope(ref TokenList input)
    {
        List<INode> statements = [];

        input.ExpectFront(TokenType.OpenBrace).RemoveFront();

        while (input.Front().Type != TokenType.CloseBrace)
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

                    statements.Add(new ReturnNode
                    {
                        Expression = ParseExpression(ref statementTokens)
                    });
                }
                    break;
                default:
                {
                    var statementTokens = ConsumeTokensTill(ref input, [TokenType.StatementEnd, TokenType.Comma]);

                    // Handle multiple declarations in one line
                    if (input.Front().Type == TokenType.Comma)
                    {
                        input.InsertFront(statementTokens);
                        statements.AddRange(ParseMultipleDeclarationLine(ref input));
                        input.ExpectFront(TokenType.StatementEnd).RemoveFront();
                    }
                    else
                    {
                        input.ExpectFront(TokenType.StatementEnd).RemoveFront();

                        statements.Add(ParseExpression(ref statementTokens));
                    }
                }
                    break;
            }

        input.RemoveFront();

        return new ScopeNode
        {
            Statements = statements.ToArray()
        };
    }

    public static int ParseCount(ref TokenList input)
    {
        var typeCount = 1;

        if (input.NotEmpty() && input.Front().Type == TokenType.OpenBracket)
        {
            input.RemoveFront();
            typeCount = input.Front().Type == TokenType.Numeric ? int.Parse(input.RemoveFront().Value) : 0;
            input.ExpectFront(TokenType.CloseBracket).RemoveFront();
        }

        return typeCount;
    }

    public static IType ParseType(ref TokenList input)
    {
        var token = input.RemoveFront();
        IType type = BuiltInTypeNode.IsBuiltInType(token.Type)
            ? new BuiltInTypeNode(token.Type)
            : new UnknownType(token.Value);
        while (input.NotEmpty() && input.Front().Type == TokenType.OpMultiply)
        {
            input.RemoveFront();
            type = new PointerNode
            {
                Type = type
            };
        }

        return type;
    }

    public static void ParseVariableModifiers(ref TokenList input, out bool isConstant,
        out bool isStatic)
    {
        if (input.Front().Type == TokenType.Const)
        {
            input.RemoveFront();
            isConstant = true;
        }
        else
        {
            isConstant = false;
        }

        if (input.Front().Type == TokenType.Static)
        {
            input.RemoveFront();
            isStatic = true;
        }
        else
        {
            isStatic = false;
        }
    }

    public static string ParseVariableName(ref TokenList input)
    {
        return input.ExpectFront(TokenType.Unknown).RemoveFront().Value;
    }

    public static VariableDeclarationNode ParseVariableDeclaration(ref TokenList input)
    {
        ParseVariableModifiers(ref input, out var isConst, out var isStatic);

        var type = ParseType(ref input);

        var name = ParseVariableName(ref input);

        var count = ParseCount(ref input);

        return new VariableDeclarationNode
        {
            IsConstant = isConst,
            IsStatic = isStatic,
            Type = type,
            Name = name,
            Count = count
        };
    }

    public static StructVariableDeclarationNode ParseStructDeclaration(ref TokenList input)
    {
        var type = ParseType(ref input);

        var name = input.ExpectFront(TokenType.Unknown).RemoveFront().Value;

        var count = ParseCount(ref input);

        var alias = string.Empty;

        if (input.NotEmpty() && input.Front().Type == TokenType.Colon)
        {
            input.RemoveFront();
            alias = input.ExpectFront(TokenType.Unknown).RemoveFront().Value;
        }

        return new StructVariableDeclarationNode
        {
            Type = type,
            Name = name,
            Count = count,
            Mapping = alias
        };
    }


    public static ParameterDeclarationNode ParseFunctionArgument(ref TokenList input)
    {
        var isInput = true;

        if (input.Front().Type is TokenType.DataIn or TokenType.DataOut)
            isInput = input.RemoveFront().Type == TokenType.DataIn;

        var type = input.RemoveFront();

        var typeCount = 1;

        if (input.NotEmpty() && input.Front().Type == TokenType.OpenBracket)
        {
            input.RemoveFront();
            typeCount = input.Front().Type == TokenType.Numeric ? int.Parse(input.RemoveFront().Value) : 0;
            input.ExpectFront(TokenType.CloseBracket).RemoveFront();
        }

        var name = input.RemoveFront().Value;

        return new ParameterDeclarationNode
        {
            IsInput = isInput,
            Name = name,
            Type = BuiltInTypeNode.IsBuiltInType(type.Type)
                ? new BuiltInTypeNode(type.Type)
                : new UnknownType(type.Value),
            Count = typeCount
        };
    }

    public static FunctionNode ParseFunction(ref TokenList input)
    {
        input.ExpectFront(TokenType.Function).RemoveFront();

        var name = input.ExpectFront(TokenType.Unknown).RemoveFront();

        input.ExpectFront(TokenType.OpenParen).RemoveFront();
        var allArgsTokens = ConsumeTokensTill(ref input, [TokenType.CloseParen], 1);
        input.ExpectFront(TokenType.CloseParen).RemoveFront();

        List<ParameterDeclarationNode> args = [];

        while (allArgsTokens.NotEmpty())
        {
            var argsTokens = ConsumeTokensTill(ref allArgsTokens, [TokenType.Comma]);

            if (allArgsTokens.NotEmpty()) allArgsTokens.ExpectFront(TokenType.Comma).RemoveFront();

            args.Add(ParseFunctionArgument(ref argsTokens));
        }

        input.ExpectFront(TokenType.Colon).RemoveFront();
        var type = input.RemoveFront();

        IType returnType = BuiltInTypeNode.IsBuiltInType(type.Type)
            ? new BuiltInTypeNode(type.Type)
            : new UnknownType(type.Value);

        if (input.Front().Type == TokenType.Arrow)
        {
            input.RemoveFront();
            var expr = ConsumeTokensTill(ref input, [TokenType.StatementEnd]);
            input.ExpectFront(TokenType.StatementEnd).RemoveFront();

            return new FunctionNode
            {
                Name = name.Value,
                Params = args.ToArray(),
                ReturnType = returnType,
                Scope = new ScopeNode
                {
                    Statements =
                    [
                        new ReturnNode
                        {
                            Expression = ParseExpression(ref expr)
                        }
                    ]
                }
            };
        }

        return new FunctionNode
        {
            Name = name.Value,
            Params = args.ToArray(),
            ReturnType = returnType,
            Scope = ParseScope(ref input)
        };
    }

    public static INode[] ParseGlobalScope(ref TokenList input)
    {
        if (input.Empty()) return [];
        List<INode> statements = [];
        while (input.NotEmpty())
            switch (input.Front().Type)
            {
                case TokenType.NamedScopeBegin:
                    statements.Add(ParseNamedScope(ref input));
                    break;
                case TokenType.Include:
                    statements.Add(ParseInclude(ref input));
                    break;
                case TokenType.Define:
                    statements.Add(ParseDefine(ref input));
                    break;
                case TokenType.Uniform:
                    statements.Add(ParseUniform(ref input));
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
                case TokenType.Function:
                    statements.Add(ParseFunction(ref input));
                    break;
                default:
                    throw new ParserException(input.Front().DebugInfo, "Unknown node type");
            }

        return statements.ToArray();
    }

    public static INode[] Parse(ref TokenList input)
    {
        return ParseGlobalScope(ref input);
    }
}