namespace rsl.Nodes;

public class PushConstantNode : Node
{
    public readonly DeclarationNode[] Declarations;

    public Dictionary<string, string> Tags;

    public PushConstantNode(IEnumerable<DeclarationNode> data, Dictionary<string, string> tags) : base(
        Nodes.NodeType.PushConstant)
    {
        Declarations = data.ToArray();
        Tags = tags;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Declarations;
    }
    
    public int SizeOf() => Declarations.Aggregate(0, (total, decl) => total + decl.SizeOf());
}