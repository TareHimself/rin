namespace rsl.Nodes;

public class BlockDeclarationNode : DeclarationNode
{
    public readonly DeclarationNode[] Declarations;
    
    public BlockDeclarationNode(string declarationName, int count,IEnumerable<DeclarationNode> declarations) : base(DeclarationType.Block, declarationName, count)
    {
        Declarations = declarations.ToArray();
    }

    public override int SizeOf() => Declarations.Aggregate(0, (total, decl) => total + decl.SizeOf());

    public override IEnumerable<Node> GetChildren() => Declarations;

    public override string GetTypeName() => "_block_" + DeclarationName;
}