namespace rsl.Nodes;

public class DiscardNode : Node
{
    public DiscardNode() : base(NodeType.Discard)
    {
    }
    
    public override IEnumerable<Node> GetChildren() => [];
}