namespace rsl.Nodes;

/// <summary>
///     <see cref="Identifier" />( <see cref="Arguments" /> )
/// </summary>
public class CallNode : Node
{
    public Node[] Arguments;
    public IdentifierNode Identifier;

    public CallNode(IdentifierNode identifier, IEnumerable<Node> arguments) : base(Nodes.NodeType.Call)
    {
        Identifier = identifier;
        Arguments = arguments.ToArray();
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Arguments;
    }
}