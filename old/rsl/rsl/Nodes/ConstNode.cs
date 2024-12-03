namespace rsl.Nodes;

/// <summary>
///     const <see cref="Declaration" />
/// </summary>
public class ConstNode : Node
{
    public DeclarationNode Declaration;

    public ConstNode(DeclarationNode declaration) : base(Nodes.NodeType.Const)
    {
        Declaration = declaration;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Declaration];
    }
}