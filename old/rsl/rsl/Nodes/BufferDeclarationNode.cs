namespace rsl.Nodes;

public class BufferDeclarationNode : DeclarationNode
{
    public readonly DeclarationNode[] Declarations;
    
    public BufferDeclarationNode(string declarationName, int count,IEnumerable<DeclarationNode> declarations) : base(Nodes.DeclarationType.Buffer, declarationName, count)
    {
        Declarations = declarations.ToArray();
    }

    public override int SizeOf() => Declarations.Aggregate(0, (total, decl) => total + decl.SizeOf());

    public override IEnumerable<Node> GetChildren() => Declarations;

    public override string GetTypeName() => "";
}