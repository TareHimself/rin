namespace Rin.Shading.Ast.Nodes;

public class UnknownType(string typeName) : IType
{
    public string TypeName { get; set; } = typeName;
    
    public IEnumerable<INode> Children { get; } = [];
    public ulong GetSize()
    {
        throw new Exception("Cannot Get Size For Unknown Type");
    }
}