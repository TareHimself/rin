namespace rsl.Nodes;

/// <summary>
///     return <see cref="Expression" />
/// </summary>
public class ReturnNode : Node
{
    public Node Expression;

    public ReturnNode(Node expression) : base(Nodes.NodeType.Return)
    {
        Expression = expression;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Expression];
    }
}