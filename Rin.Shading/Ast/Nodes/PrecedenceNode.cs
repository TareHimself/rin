namespace Rin.Shading.Ast.Nodes;

public class PrecedenceNode : INode
{
    public required INode Expression { get; set; }
    public IEnumerable<INode> Children => [Expression];
}