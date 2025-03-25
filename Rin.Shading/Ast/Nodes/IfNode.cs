namespace Rin.Shading.Ast.Nodes;

public class IfNode : INode
{
    public required INode Condition { get; set; }
    public required INode Scope { get; set; }
    public INode? Else { get; set; }

    public IEnumerable<INode> Children
    {
        get
        {
            yield return Condition;
            yield return Scope;
            if (Else is not null)
            {
                yield return Else;
            }
        }
    }
}