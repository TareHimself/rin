namespace Rin.Shading.Ast.Nodes;

public class ContinueNode : INode
{
    public IEnumerable<INode> Children { get; } = ArraySegment<INode>.Empty;
}