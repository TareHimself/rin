namespace Rin.Shading.Ast.Nodes;

public class ParameterDeclarationNode : DeclarationNode
{
    public override required string Name { get; set; }
    public override required IType Type { get; set; }
    public override required int Count { get; set; }

    public bool IsInput { get; set; } = true;
}