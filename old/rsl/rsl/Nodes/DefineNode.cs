namespace rsl.Nodes;

public class DefineNode : Node
{
    public string Identifier;
    public Node Expression;
    public DefineNode(string identifier,Node expression) : base(NodeType.Define)
    {
        Identifier = identifier;
        Expression = expression;
    }

    public override IEnumerable<Node> GetChildren() => [Expression];
}