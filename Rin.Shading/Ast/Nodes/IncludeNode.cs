namespace Rin.Shading.Ast.Nodes;

public class IncludeNode : INode
{
    public required string Path { get; set; }
    public IEnumerable<INode> Children { get; } = [];
}