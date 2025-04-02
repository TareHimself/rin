namespace Rin.Shading.Ast.Nodes;

public class DecrementNode : INode
{
    public required INode Expression { get; set; }

    public bool Before { get; set; }
    public IEnumerable<INode> Children => [Expression];
}