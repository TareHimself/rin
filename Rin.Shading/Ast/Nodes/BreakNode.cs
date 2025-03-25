namespace Rin.Shading.Ast.Nodes;

public class BreakNode : INode
{
    public IEnumerable<INode> Children { get; } = ArraySegment<INode>.Empty;
}