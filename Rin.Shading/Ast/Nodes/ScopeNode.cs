namespace Rin.Shading.Ast.Nodes;

public class ScopeNode : INode
{
    public IEnumerable<INode> Children => Statements;

    public INode[] Statements { get; set; } = [];
}