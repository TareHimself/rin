namespace Rin.Shading.Ast.Nodes;

public class IdentifierNode : INode
{
    public required string Value { get; set; }
    public IEnumerable<INode> Children { get; } = [];
}