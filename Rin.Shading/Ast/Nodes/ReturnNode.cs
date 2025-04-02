namespace Rin.Shading.Ast.Nodes;

public class ReturnNode : INode
{
    public required INode Expression { get; set; }
    public IEnumerable<INode> Children => [Expression];
}