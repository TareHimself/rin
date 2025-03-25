namespace Rin.Shading.Ast.Nodes;

public class PointerNode : IType
{
    public required IType Type { get; set; }
    
    public IEnumerable<INode> Children => [Type];


    public ulong GetSize()
    {
        unsafe
        {
            return (ulong)sizeof(nuint);
        }
    }
}