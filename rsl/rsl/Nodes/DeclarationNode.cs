namespace rsl.Nodes;

/// <summary>
///     <see cref="Nodes.DeclarationType">Type</see> <see cref="DeclarationName" />[<see cref="Count" />]
/// </summary>
public class DeclarationNode : Node
{
    public readonly int Count;
    public readonly DeclarationType DeclarationType;
    public string DeclarationName;
    
    public static DeclarationType TokenTypeToDeclarationType(TokenType tokenType)
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
            TokenType.TypeBoolean => DeclarationType.Boolean,
            _ => throw new Exception("Unknown token type")
        };
    }

    public DeclarationNode(DeclarationType declarationType, string declarationName, int count) : base(Nodes.NodeType.Declaration)
    {
        DeclarationType = declarationType;
        DeclarationName = declarationName;
        Count = count;
    }
    
    public DeclarationNode(TokenType tokenType, string declarationName, int count) : base(Nodes.NodeType.Declaration)
    {
        DeclarationType = TokenTypeToDeclarationType(tokenType);
        DeclarationName = declarationName;
        Count = count;
    }

    public virtual int SizeOf()
    {
        return DeclarationType switch
        {
            DeclarationType.Struct => throw new Exception(
                "Node with type 'Struct' must use class 'StructDeclarationNode'"),
            DeclarationType.Float => 4 * Count,
            DeclarationType.Int => 4 * Count,
            DeclarationType.Float2 => 8 * Count,
            DeclarationType.Int2 => 8 * Count,
            DeclarationType.Float3 => 12 * Count,
            DeclarationType.Int3 => 12 * Count,
            DeclarationType.Float4 => 16 * Count,
            DeclarationType.Int4 => 16 * Count,
            DeclarationType.Mat3 => 12 * 3 * Count,
            DeclarationType.Mat4 => 16 * 4 * Count, 
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public virtual string GetTypeName()
    {
        return DeclarationType switch
        {
            DeclarationType.Float => "float",
            DeclarationType.Int => "int",
            DeclarationType.Float2 => "float2",
            DeclarationType.Int2 => "int2",
            DeclarationType.Float3 =>  "float3",
            DeclarationType.Int3 =>  "int3",
            DeclarationType.Float4 =>  "float4",
            DeclarationType.Int4 => "int4",
            DeclarationType.Mat3 => "mat3",
            DeclarationType.Mat4 => "mat4",
            DeclarationType.Void => "void",
            DeclarationType.Sampler2D => "sampler2D",
            DeclarationType.Buffer => "buffer",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override IEnumerable<Node> GetChildren() => [];
}