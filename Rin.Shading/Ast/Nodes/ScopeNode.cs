namespace Rin.Shading.Ast.Nodes;

public class ScopeNode : INode
{
    public INode[] Statements { get; set; } = [];
    public IEnumerable<INode> Children => Statements;
}