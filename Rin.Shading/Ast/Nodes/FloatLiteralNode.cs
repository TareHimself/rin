namespace Rin.Shading.Ast.Nodes;

public class FloatLiteralNode : INode
{
    public required string Value { get; set; }
    public IEnumerable<INode> Children { get; } = [];
}