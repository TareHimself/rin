namespace rsl.Nodes;

public class FloatLiteral : Node
{
    public float Value;

    public FloatLiteral(float value) : base(Nodes.NodeType.FloatLiteral)
    {
        Value = value;
    }

    public override IEnumerable<Node> GetChildren() => [];
}