namespace Rin.Shading.Ast.Nodes;

public class ConditionalNode : Operator
{
    public required INode Condition { get; set; }
    public required INode Left { get; set; }
    public required INode Right { get; set; }

    public override IEnumerable<INode> Children => [Condition, Left, Right];
}