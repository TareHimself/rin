namespace Rin.Shading.Ast.Nodes;

public class DefineNode : INode
{
    public required string Name { get; set; }
    public required INode Expression { get; set; }
    
    public IEnumerable<INode> Children => [Expression];
}