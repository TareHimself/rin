namespace Rin.Shading.Ast.Nodes;

public class NamedScopeNode : INode
{
    public required string Name { get; set; }
    public required INode[] Statements { get; set; }

    public Dictionary<string, string> Tags { get; set; } = [];

    public IEnumerable<INode> Children => Statements;
}