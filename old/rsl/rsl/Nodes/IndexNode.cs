namespace rsl.Nodes;

/// <summary>
///     <see cref="HasLeftNode.Left" />[<see cref="IndexExpression" />]
/// </summary>
public class IndexNode : HasLeftNode
{
    public Node IndexExpression;

    public IndexNode(Node left, Node indexExpression) : base(left, Nodes.NodeType.Index)
    {
        IndexExpression = indexExpression;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Left, IndexExpression];
    }
    
}