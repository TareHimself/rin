namespace Rin.Shading.Ast.Nodes;

public abstract class Operator : INode
{
    public abstract IEnumerable<INode> Children { get; }
}