namespace Rin.Shading.Ast.Nodes;

public class BinaryOperatorNode : Operator
{
    public required INode Left { get; set; }
    public required INode Right { get; set; }
    public required BinaryOperator Operator { get; set; }
    public override IEnumerable<INode> Children => [Left, Right];
}