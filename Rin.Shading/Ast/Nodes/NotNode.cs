namespace Rin.Shading.Ast.Nodes;

public class NotNode : Operator
{
    public required INode Expression { get; set; }
    public override IEnumerable<INode> Children => [Expression];
}