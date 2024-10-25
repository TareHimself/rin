namespace rsl.Nodes;

/// <summary>
///     <see cref="Value" />
/// </summary>
public class BooleanLiteral : Node
{
    public bool Value;

    public BooleanLiteral(bool value) : base(Nodes.NodeType.BooleanLiteral)
    {
        Value = value;
    }

    public override IEnumerable<Node> GetChildren() => [];
}