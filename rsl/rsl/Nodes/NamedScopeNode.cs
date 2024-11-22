
namespace rsl.Nodes;

/// <summary>
///     @<see cref="Name">ScopeName</see> {
///     <see cref="ScopeNode.Statements" />
///     }
/// </summary>
public class NamedScopeNode : ScopeNode
{
    public readonly ScopeType ScopeType;

    public NamedScopeNode(TokenType scopeType, IEnumerable<Node> nodes) : base(nodes)
    {
        NodeType = Nodes.NodeType.NamedScope;
        ScopeType = scopeType switch
        {
            TokenType.VertexScope => ScopeType.Vertex,
            TokenType.FragmentScope => ScopeType.Fragment,
            _ => throw new ArgumentOutOfRangeException(nameof(scopeType), scopeType, null)
        };
    }


    public NamedScopeNode(ScopeType scopeType, IEnumerable<Node> nodes) : base(nodes)
    {
        NodeType = Nodes.NodeType.NamedScope;
        ScopeType = scopeType;
    }
}