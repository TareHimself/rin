namespace Rin.Shading.Ast.Nodes;

public class DecrementNode : INode
{
    public IEnumerable<INode> Children => [Expression];
    public required INode Expression { get; set; }
    
    public bool Before { get; set; }
}