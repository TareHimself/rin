namespace Rin.Shading.Ast.Nodes;

public class BooleanLiteralNode : INode
{
    public required bool Value { get; set; }
    public IEnumerable<INode> Children { get; } = [];
}