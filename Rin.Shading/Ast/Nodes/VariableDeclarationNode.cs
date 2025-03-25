namespace Rin.Shading.Ast.Nodes;

public class VariableDeclarationNode : DeclarationNode
{
    public bool IsConstant { get; set; }
    public bool IsStatic { get; set; }
    public override required string Name { get; set; }
    public override required IType Type { get; set; }
    public override required int Count { get; set; }
}