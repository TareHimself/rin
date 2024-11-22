namespace rsl.Nodes;

public class HasLeftNode : Node
{
    public Node Left;

    public HasLeftNode(Node left, NodeType nodeType) : base(nodeType)
    {
        Left = left;
    }

    public override IEnumerable<Node> GetChildren() => [Left];
}