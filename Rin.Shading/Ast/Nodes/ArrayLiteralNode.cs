namespace Rin.Shading.Ast.Nodes;

public class ArrayLiteralNode : INode
{
    public required INode[] Items { get; set; }
    public IEnumerable<INode> Children => Items;
}