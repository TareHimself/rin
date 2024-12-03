namespace rsl.Nodes;

/// <summary>
///     <see cref="Value" />
/// </summary>
public class IntLiteral : Node
{
    public int Value;

    public IntLiteral(int value) : base(Nodes.NodeType.IntLiteral)
    {
        Value = value;
    }

    public override IEnumerable<Node> GetChildren() => [];
}