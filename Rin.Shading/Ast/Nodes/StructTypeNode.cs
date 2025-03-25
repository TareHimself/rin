namespace Rin.Shading.Ast.Nodes;

public class StructTypeNode : IType
{
    public required StructNode Struct { get; set; }

    public ulong GetSize()
    {
        return Struct.GetSize();
    }
    
    public IEnumerable<INode> Children { get; } = [];
}