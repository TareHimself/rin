namespace rsl.Nodes;

public class DecrementNode : Node
{
    public bool IsPre;
    public Node Target;

    public DecrementNode(bool isPre,Node target) : base(NodeType.Decrement)
    {
        Target = target;
        IsPre = isPre;
    }


    public override IEnumerable<Node> GetChildren() => [Target];
}