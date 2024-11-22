namespace rsl.Nodes;

public class IfNode : Node
{
    public Node Condition;
    public ScopeNode Scope;
    public Node Else;
    
    public IfNode(Node condition,ScopeNode scope) : base(Nodes.NodeType.If)
    {
        Condition = condition;
        Scope = scope;
        Else = new NoOpNode();
    }
    public IfNode(Node condition,ScopeNode scope,Node elseNode) : base(Nodes.NodeType.If)
    {
        Condition = condition;
        Scope = scope;
        Else = elseNode;
    }


    public override IEnumerable<Node> GetChildren() => [Condition, Scope, Else];
}