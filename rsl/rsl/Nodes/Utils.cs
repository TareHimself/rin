

namespace rsl.Nodes;

public static class Utils
{
    public static DeclarationType? TokenTypeToDeclarationType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.TypeFloat => DeclarationType.Float,
            TokenType.TypeFloat2 => DeclarationType.Float2,
            TokenType.TypeFloat3 => DeclarationType.Float3,
            TokenType.TypeFloat4 => DeclarationType.Float4,
            TokenType.TypeInt => DeclarationType.Int,
            TokenType.TypeInt2 => DeclarationType.Int2,
            TokenType.TypeInt3 => DeclarationType.Int3,
            TokenType.TypeInt4 => DeclarationType.Int4,
            TokenType.TypeMat3 => DeclarationType.Mat3,
            TokenType.TypeMat4 => DeclarationType.Mat4,
            TokenType.TypeVoid => DeclarationType.Void,
            TokenType.TypeSampler2D => DeclarationType.Sampler2D,
            _ => null
        };
    }
}