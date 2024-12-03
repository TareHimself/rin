namespace rsl.Nodes;

public class NoOpNode : Node
{
    public NoOpNode() : base(Nodes.NodeType.NoOp)
    {
    }

    public override IEnumerable<Node> GetChildren() => [];
}