namespace rsl.Nodes;

public class ConditionalNode : HasLeftNode
{
    public Node Condition;
    public Node Right;

    public ConditionalNode(Node condition,Node left, Node right) : base(left, Nodes.NodeType.Conditional)
    {
        Condition = condition;
        Right = right;
    }

    public override IEnumerable<Node> GetChildren() => [Condition, Left, Right];
}