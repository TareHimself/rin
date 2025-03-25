namespace Rin.Shading.Ast.Nodes;

public class PrecedenceNode : INode
{
    public IEnumerable<INode> Children => [Expression];
    public required INode Expression { get; set; }
}