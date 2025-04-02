namespace Rin.Shading.Ast.Nodes;

public class AsNode : INode
{
    public required INode Target { get; set; }
    public required IType Type { get; set; }
    public IEnumerable<INode> Children => [Target, Type];
}