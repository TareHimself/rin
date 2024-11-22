using System.Reflection;

namespace rsl;

public class Token(TokenType type, string value, DebugInfo debugInfo)
{
    private static readonly Dictionary<string, TokenType> KeywordToTokenTypeMap = BuildTokenMap();
    public static readonly SortedDictionary<int, HashSet<string>> TokenSizesToKeywords = BuildSizesMap();

    public string Value = value;
    public DebugInfo DebugInfo = debugInfo;
    public TokenType Type = type;


    public Token(TokenType type, uint line, uint col) : this(type, new DebugInfo(line, col))
    {
    }

    public Token(TokenType type, DebugInfo debugInfo) : this(type, TokenTypeToKeyword(type) ?? "", debugInfo)
    {
    }

    public Token(string value, Token other) : this(KeywordToTokenType(value) ?? TokenType.Unknown, value, other.DebugInfo)
    {
    }
    
    public Token(string value, DebugInfo debugInfo) : this(KeywordToTokenType(value) ?? TokenType.Unknown, value, debugInfo)
    {
    }
    

    public Token(Token other) : this(other.Type, other.Value, other.DebugInfo)
    {
    }

   


    public static string? TokenTypeToKeyword(TokenType type)
    {
        var enumType = typeof(TokenType);
        var memberInfos =
            enumType.GetMember(type.ToString());
        var enumValueMemberInfo = memberInfos.FirstOrDefault(m =>
            m.DeclaringType == enumType);
        var valueAttribute =
            (KeywordTokenAttribute?)enumValueMemberInfo?
                .GetCustomAttribute(typeof(KeywordTokenAttribute), false);
        return valueAttribute?.Keyword;
    }

    public static Dictionary<string, TokenType> BuildTokenMap()
    {
        var tokenMap = new Dictionary<string, TokenType>();

        foreach (var token in (TokenType[])Enum.GetValues(typeof(TokenType)))
        {
            var tok = TokenTypeToKeyword(token);
            if (tok == null) continue;
            tokenMap.Add(tok, token);
        }

        return tokenMap;
    }
    
    
    public static SortedDictionary<int, HashSet<string>> BuildSizesMap()
    {
        var sizeMap = new SortedDictionary<int, HashSet<string>>();

        foreach (var token in (TokenType[])Enum.GetValues(typeof(TokenType)))
        {
            var tok = TokenTypeToKeyword(token);
            if (tok == null) continue;
            if (sizeMap.TryGetValue(tok.Length, out var value))
            {
                value.Add(tok);
            }
            else
            {
                sizeMap.Add(tok.Length, [tok]);
            }
        }

        return sizeMap;
    }

    public static TokenType? KeywordToTokenType(string keyword)
    {
        return KeywordToTokenTypeMap.TryGetValue(keyword, out var tokType) ? tokType : null;
    }
}