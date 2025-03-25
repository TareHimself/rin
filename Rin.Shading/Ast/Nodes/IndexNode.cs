namespace Rin.Shading.Ast.Nodes;

public class IndexNode : INode
{
    public IEnumerable<INode> Children => [Target, Expression];
    public required INode Target { get; set; }
    
    public required INode Expression { get; set; }
}