namespace rsl.Nodes;

public abstract class Node
{
    public NodeType NodeType;

    public Node(NodeType type)
    {
        NodeType = type;
    }


    public abstract IEnumerable<Node> GetChildren();
}