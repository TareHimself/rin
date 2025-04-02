namespace Rin.Shading.Ast.Nodes;

public interface INode
{
    public IEnumerable<INode> Children { get; }
}