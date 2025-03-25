namespace Rin.Shading.Ast.Nodes;

public class ForNode : INode
{
    public required INode Init { get; set; }
    public required INode Condition { get; set; }
    public required INode Update { get; set; }
    public required ScopeNode Scope { get; set; }
    public IEnumerable<INode> Children => [Init, Condition, Update, Scope];
}