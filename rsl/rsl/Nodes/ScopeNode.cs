namespace rsl.Nodes;

/// <summary>
///     {
///     <see cref="Statements" />
///     }
/// </summary>
public class ScopeNode : Node
{
    public Node[] Statements;

    public ScopeNode(IEnumerable<Node> statements) : base(Nodes.NodeType.Scope)
    {
        Statements = statements.ToArray();
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Statements;
    }
}