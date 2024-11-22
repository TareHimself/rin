namespace rsl.Nodes;

/// <summary>
///     <see cref="StructName" /> <see cref="DeclarationNode.DeclarationName" />
/// </summary>
public class StructDeclarationNode : DeclarationNode
{
    public StructNode? Struct;
    public string StructName;

    public StructDeclarationNode(string structName, string declarationName,int count) : base(DeclarationType.Struct, declarationName, count)
    {
        StructName = structName;
    }

    public StructDeclarationNode(StructNode inStruct, string declarationName, int count) : this(inStruct.Name, declarationName,count)
    {
        Struct = inStruct;
        StructName = inStruct.Name;
    }

    public override int SizeOf()
    {
        if (Struct == null) return sizeof(ulong);
        return Struct.Declarations.Aggregate(0, (total, node) => total + node.SizeOf());
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Struct != null ? [Struct] : [];
    }

    public override string GetTypeName() => StructName;
}