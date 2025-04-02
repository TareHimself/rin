namespace Rin.Shading.Ast.Nodes;

public class IntLiteralNode : INode
{
    public required int Value { get; set; }
    public IEnumerable<INode> Children { get; } = [];
}