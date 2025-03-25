namespace Rin.Shading.Ast.Nodes;

public class BuiltInTypeNode(TokenType type) : IType
{
    public TokenType Type => type;

    public string Value { get; set; } = Token.TokenTypeToKeyword(type) ?? string.Empty;

    public ulong GetSize()
    {
        return Type switch
        {
            TokenType.TypeFloat or TokenType.TypeInt => 4,
            TokenType.TypeFloat2 or TokenType.TypeInt2 => 8,
            TokenType.TypeFloat3 or TokenType.TypeInt3 => 12,
            TokenType.TypeFloat4 or TokenType.TypeInt4 => 16,
            TokenType.TypeFloat3X3 => 36,
            TokenType.TypeFloat4X4 => 64,
            TokenType.TypeBoolean => 4, // Not Sure
            TokenType.TypeVar or TokenType.TypeVoid or TokenType.TypeTexture or TokenType.TypeCubemap or TokenType.TypeImage =>
                throw new Exception("cannot get size of var"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool IsBuiltInType(TokenType type)
    {
        return type switch
        {
            TokenType.TypeVar or TokenType.TypeVoid or TokenType.TypeTexture or TokenType.TypeCubemap or TokenType.TypeImage
                or TokenType.TypeFloat or TokenType.TypeInt or TokenType.TypeFloat2 or TokenType.TypeInt2
                or TokenType.TypeFloat3 or TokenType.TypeInt3 or TokenType.TypeFloat4 or TokenType.TypeInt4
                or TokenType.TypeFloat3X3 or TokenType.TypeFloat4X4 or TokenType.TypeBoolean => true,
            _ => false
        };
    }

    public IEnumerable<INode> Children { get; } = [];
}