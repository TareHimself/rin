namespace Rin.Shading.Ast.Nodes;

public class BooleanLiteralNode : INode
{
    public IEnumerable<INode> Children { get; } = [];
    public required bool Value { get; set; }
}