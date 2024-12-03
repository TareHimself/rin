namespace rsl.Nodes;

/// <summary>
///     <see cref="Identity" />
/// </summary>
public class IdentifierNode : Node
{
    public string Identity;

    public IdentifierNode(string identity) : base(Nodes.NodeType.Identifier)
    {
        Identity = identity;
    }

    public override IEnumerable<Node> GetChildren() => [];
}