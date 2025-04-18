﻿namespace Rin.Shading;

public static class Tokenizer
{
    private static Token? JoinTokensTill(ref TokenList tokens, params string[] search)
    {
        if (search.Length == 0) return null;
        if (search.Contains(tokens.Front().Value)) return null;

        var pending = tokens.RemoveFront();
        while (tokens.NotEmpty() && !search.Contains(tokens.Front().Value))
        {
            var front = tokens.RemoveFront();
            pending = new Token(pending.Value + front.Value, pending.DebugInfo + front.DebugInfo);
        }

        return pending;
    }

    private static bool IsSplitToken(Token token)
    {
        return token.Type switch
        {
            TokenType.OpenBrace or TokenType.OpenParen or TokenType.OpenBracket or TokenType.CloseBrace
                or TokenType.CloseParen or TokenType.CloseBracket or TokenType.Comma or TokenType.StatementEnd
                or TokenType.Access or TokenType.Arrow or TokenType.NamedScopeBegin => true,
            _ => false
        };
    }

    private static bool IsSeparatorToken(Token token)
    {
        return token.Type switch
        {
            TokenType.OpSubtract or TokenType.OpAdd or TokenType.OpDivide or TokenType.OpMultiply
                or TokenType.Assign => true,
            _ => IsSplitToken(token) || token.Value is " " or "\n" or "\r"
        };
    }


    public static TokenList Run(string fileName, Stream content)
    {
        var rawTokens = new TokenList();

        {
            uint lineNo = 1;
            using var reader = new StreamReader(content);
            while (reader.ReadLine() is { } line)
            {
                uint colNo = 1;
                foreach (var col in line)
                {
                    rawTokens.InsertBack(new Token(col.ToString(),
                        new DebugInfo(fileName, lineNo, colNo, lineNo, colNo + 1)));
                    colNo++;
                }

                lineNo++;
            }
        }


        var result = new TokenList();


        while (rawTokens.NotEmpty())
        {
            var curToken = rawTokens.Front();
            if (curToken.Value is " " or "\n" or "\r")
            {
                rawTokens.RemoveFront();
                continue;
            }

            if (curToken.Value is "\"" or "\'")
            {
                var token = rawTokens.RemoveFront();
                var consumed = JoinTokensTill(ref rawTokens, token.Value);
                rawTokens.RemoveFront();
                if (consumed != null)
                    result.InsertBack(new Token(TokenType.String, consumed.Value, consumed.DebugInfo));
                continue;
            }

            if (curToken.Value == "/")
            {
                rawTokens.RemoveFront();

                if (rawTokens.NotEmpty())
                {
                    var nextToken = rawTokens.Front();
                    if (nextToken.Value == "/")
                    {
                        rawTokens.RemoveFront();
                        var beginLine = nextToken.DebugInfo.BeginLine;
                        var combined = rawTokens.RemoveFront();
                        while (rawTokens.NotEmpty() && rawTokens.Front().DebugInfo.BeginLine == beginLine)
                        {
                            var next = rawTokens.RemoveFront();
                            combined = new Token(combined.Value + next.Value, combined.DebugInfo + next.DebugInfo);
                        }

                        continue;
                    }

                    if (nextToken.Value == "*")
                    {
                        rawTokens.RemoveFront();
                        JoinTokensTill(ref rawTokens, "*/");
                        continue;
                    }
                }

                rawTokens.InsertFront(curToken);
            }

            var maxSize = Token.TokenSizesToKeywords.Keys.Max();

            var combinedString = "";
            var searchTokens = new TokenList();

            while (combinedString.Length < maxSize && rawTokens.NotEmpty())
            {
                var front = rawTokens.Front();
                searchTokens.InsertBack(rawTokens.RemoveFront());
                combinedString += front.Value;
            }

            var matchedSize = false;

            foreach (var (size, matches) in Token.TokenSizesToKeywords.Reverse())
            {
                while (combinedString.Length > size)
                {
                    var back = searchTokens.RemoveBack();
                    rawTokens.InsertFront(back);
                    combinedString = combinedString[..^back.Value.Length];
                }

                if (combinedString.Length < size) continue;

                if (matches.Contains(combinedString) && (IsSeparatorToken(new Token(combinedString, new DebugInfo())) ||
                                                         rawTokens.Empty() || IsSeparatorToken(rawTokens.Front())))
                {
                    matchedSize = true;
                    break;
                }
            }

            if (matchedSize)
            {
                var debugSpan = searchTokens.Front().DebugInfo;
                searchTokens.RemoveFront();
                while (searchTokens.NotEmpty())
                {
                    debugSpan += searchTokens.Front().DebugInfo;
                    searchTokens.RemoveFront();
                }

                result.InsertBack(new Token(combinedString, debugSpan));
            }
            else
            {
                if (searchTokens.NotEmpty()) rawTokens.InsertFront(searchTokens);

                combinedString = rawTokens.Front().Value;
                var debugSpan = rawTokens.Front().DebugInfo;
                rawTokens.RemoveFront();
                if (int.TryParse(combinedString, out _))
                {
                    while (rawTokens.NotEmpty())
                    {
                        // Try parse decimal point
                        if (rawTokens.Front().Value == ".")
                        {
                            var point = rawTokens.RemoveFront();
                            var next = rawTokens.Front();
                            if (float.TryParse(combinedString + point.Value + next.Value, out _))
                            {
                                combinedString += point.Value + next.Value;
                                debugSpan += point.DebugInfo + next.DebugInfo;
                                rawTokens.RemoveFront();
                                continue;
                            }

                            rawTokens.InsertFront(point);
                            break;
                        }

                        if (rawTokens.Front().Value == "e")
                        {
                            var exp = rawTokens.RemoveFront();
                            if (rawTokens.Front().Type == TokenType.OpSubtract)
                            {
                                var sign = rawTokens.RemoveFront();
                                var next = rawTokens.Front();
                                if (float.TryParse(combinedString + exp.Value + sign.Value + next.Value, out _))
                                {
                                    combinedString += exp.Value + sign.Value + next.Value;
                                    debugSpan += exp.DebugInfo + sign.DebugInfo + next.DebugInfo;
                                    rawTokens.RemoveFront();
                                    continue;
                                }

                                rawTokens.InsertFront(next).InsertFront(sign);
                            }
                            else
                            {
                                var next = rawTokens.Front();
                                if (float.TryParse(combinedString + exp.Value + next, out _))
                                {
                                    combinedString += exp.Value + next.Value;
                                    debugSpan += exp.DebugInfo + next.DebugInfo;
                                    rawTokens.RemoveFront();
                                    continue;
                                }

                                rawTokens.InsertFront(next);
                            }

                            rawTokens.InsertFront(exp);
                            break;
                        }

                        if (int.TryParse(rawTokens.Front().Value, out _))
                        {
                            var tok = rawTokens.RemoveFront();
                            combinedString += tok.Value;
                            debugSpan += tok.DebugInfo;
                        }
                        else
                        {
                            break;
                        }
                    }

                    result.InsertBack(new Token(TokenType.Numeric, combinedString, debugSpan));
                }
                else
                {
                    while (rawTokens.NotEmpty() && !IsSeparatorToken(rawTokens.Front()))
                    {
                        var tok = rawTokens.RemoveFront();
                        combinedString += tok.Value;
                        debugSpan += tok.DebugInfo;
                    }

                    result.InsertBack(new Token(combinedString, debugSpan));
                }
            }
        }

        return result;
    }
}