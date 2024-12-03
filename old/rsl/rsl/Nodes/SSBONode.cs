namespace rsl.Nodes;

public class SSBONode : Node
{
    public readonly DeclarationNode[] Declarations;

    public Dictionary<string, string> Tags;

    public string Name;

    public SSBONode(string name,IEnumerable<DeclarationNode> data, Dictionary<string, string> tags) : base(
        Nodes.NodeType.SSBO)
    {
        Name = name;
        Declarations = data.ToArray();
        Tags = tags;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Declarations;
    }
    
    public int SizeOf() => Declarations.Aggregate(0, (total, decl) => total + decl.SizeOf());
}