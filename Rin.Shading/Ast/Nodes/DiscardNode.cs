namespace Rin.Shading.Ast.Nodes;

public class DiscardNode : INode
{
    public IEnumerable<INode> Children { get; } = ArraySegment<INode>.Empty;
}