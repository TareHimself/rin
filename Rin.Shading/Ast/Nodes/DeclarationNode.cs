namespace Rin.Shading.Ast.Nodes;

public abstract class DeclarationNode : INode
{
    public abstract string Name { get; set; }
    public abstract IType Type { get; set; }
    public abstract int Count { get; set; }

    public IEnumerable<INode> Children => [Type];

    public ulong GetSize()
    {
        return (ulong)Count * Type.GetSize();
    }
}