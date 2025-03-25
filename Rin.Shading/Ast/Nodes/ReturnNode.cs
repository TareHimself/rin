namespace Rin.Shading.Ast.Nodes;

public class ReturnNode : INode
{
    public IEnumerable<INode> Children => [Expression];
    public required INode Expression { get; set; }
}