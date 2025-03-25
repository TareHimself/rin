namespace Rin.Shading.Ast.Nodes;

public class NoOpNode : INode
{
    public IEnumerable<INode> Children { get; } = ArraySegment<INode>.Empty;
}