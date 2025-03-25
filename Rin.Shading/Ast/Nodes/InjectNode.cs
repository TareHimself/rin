namespace Rin.Shading.Ast.Nodes;

public class InjectNode : INode
{
    public required string Code { get; set; }
    public IEnumerable<INode> Children { get; } = [];
}