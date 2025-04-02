namespace Rin.Shading.Ast.Nodes;

public class CallNode : INode
{
    public required INode Target { get; set; }
    public required INode[] Arguments { get; set; }

    public IEnumerable<INode> Children
    {
        get
        {
            yield return Target;
            foreach (var argument in Arguments) yield return argument;
        }
    }
}