

namespace rsl.Nodes;

/// <summary>
///     <see cref="HasLeftNode.Left" /> <see cref="BinaryOp">Operation</see> <see cref="Right" />
/// </summary>
public class BinaryOpNode : HasLeftNode
{
    public BinaryOp Op;
    public Node Right;

    public BinaryOpNode(Node left, Node right, BinaryOp op) : base(left, NodeType.BinaryOp)
    {
        Right = right;
        Op = op;
    }

    public BinaryOpNode(Node left, Node right, TokenType op) : this(left, right, op switch
    {
        TokenType.OpAnd => BinaryOp.And,
        TokenType.OpOr => BinaryOp.Or,
        TokenType.OpNot => BinaryOp.Not,
        TokenType.OpEqual => BinaryOp.Equal,
        TokenType.OpNotEqual => BinaryOp.NotEqual,
        TokenType.OpGreater => BinaryOp.Greater,
        TokenType.OpGreaterEqual => BinaryOp.GreaterEqual,
        TokenType.OpLess => BinaryOp.Less,
        TokenType.OpLessEqual => BinaryOp.LessEqual,
        TokenType.OpAdd => BinaryOp.Add,
        TokenType.OpSubtract => BinaryOp.Subtract,
        TokenType.OpDivide => BinaryOp.Divide,
        TokenType.OpMultiply => BinaryOp.Multiply,
        _ => throw new Exception("Unknown Token Type")
    })
    {
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Left, Right];
    }
}