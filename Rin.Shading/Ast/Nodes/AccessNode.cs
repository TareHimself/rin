namespace Rin.Shading.Ast.Nodes;

public class AccessNode : INode
{
    public IEnumerable<INode> Children => [Target, Identifier];
    public required INode Target { get; set; }
    public required IdentifierNode Identifier { get; set; }
}