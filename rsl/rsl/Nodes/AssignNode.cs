namespace rsl.Nodes;

/// <summary>
///     <see cref="HasLeftNode.Left" /> = <see cref="Right" />
/// </summary>
public class AssignNode : HasLeftNode
{
    public Node Right;

    public AssignNode(Node left, Node right) : base(left, NodeType.Assign)
    {
        Right = right;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Left,Right];
    }
}