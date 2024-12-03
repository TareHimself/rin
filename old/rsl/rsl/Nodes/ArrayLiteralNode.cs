namespace rsl.Nodes;

/// <summary>
///     { <see cref="Expressions" /> }
/// </summary>
public class ArrayLiteralNode : Node
{
    public Node[] Expressions;

    public ArrayLiteralNode(IEnumerable<Node> expressions) : base(Nodes.NodeType.ArrayLiteral)
    {
        Expressions = expressions.ToArray();
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Expressions;
    }
}