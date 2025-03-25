namespace Rin.Shading.Ast.Nodes;

public class IntLiteralNode : INode
{
    public IEnumerable<INode> Children { get; } = [];
    public required int Value { get; set; }
}