namespace Rin.Shading.Ast.Nodes;

public class AsNode : INode
{
    public IEnumerable<INode> Children => [Target, Type];
    public required INode Target { get; set; }
    public required IType Type { get; set; }
}