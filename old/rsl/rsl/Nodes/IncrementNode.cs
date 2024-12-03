namespace rsl.Nodes;

public class IncrementNode : Node
{
    public bool IsPre;
    public Node Target;

    public IncrementNode( bool isPre,Node target) : base(Nodes.NodeType.Increment)
    {
        Target = target;
        IsPre = isPre;
    }

    public override IEnumerable<Node> GetChildren() => [Target];
}