namespace rsl.Nodes;

/// <summary>
///     ( <see cref="Expression" /> )
/// </summary>
public class PrecedenceNode : Node
{
    public Node Expression;

    public PrecedenceNode(Node expression) : base(NodeType.Precedence)
    {
        Expression = expression;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Expression];
    }
}