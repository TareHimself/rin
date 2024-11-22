namespace rsl.Nodes;

/// <summary>
///     -( <see cref="Expression" /> )
/// </summary>
public class NegateNode : Node
{
    public Node Expression;

    public NegateNode(Node expression) : base(Nodes.NodeType.Negate)
    {
        Expression = expression;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Expression];
    }
}