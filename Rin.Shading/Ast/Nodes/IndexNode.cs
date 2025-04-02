namespace Rin.Shading.Ast.Nodes;

public class IndexNode : INode
{
    public required INode Target { get; set; }

    public required INode Expression { get; set; }
    public IEnumerable<INode> Children => [Target, Expression];
}