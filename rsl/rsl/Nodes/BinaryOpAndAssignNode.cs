namespace rsl.Nodes;

public class BinaryOpAndAssignNode : HasLeftNode
{
    public Node Right;
    public BinaryOp Op;
    
    public BinaryOpAndAssignNode(Node left,Node right,BinaryOp op) : base(left, NodeType.BinaryOpAndAssign)
    {
        Right = right;
        Op = op;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Left, Right];
    }
}