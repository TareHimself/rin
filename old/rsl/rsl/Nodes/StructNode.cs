namespace rsl.Nodes;

/// <summary>
///     struct <see cref="Name" /> {
///     <see cref="Declarations" />
///     }
/// </summary>
public class StructNode : Node
{
    public DeclarationNode[] Declarations;
    public string Name;

    public StructNode(string name, IEnumerable<DeclarationNode> declarations) : base(NodeType.Struct)
    {
        Name = name;
        Declarations = declarations.ToArray();
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Declarations;
    }
}