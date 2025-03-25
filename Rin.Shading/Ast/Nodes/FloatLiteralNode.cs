namespace Rin.Shading.Ast.Nodes;

public class FloatLiteralNode : INode
{
    public IEnumerable<INode> Children { get; } = [];
    public required string Value { get; set; }
}