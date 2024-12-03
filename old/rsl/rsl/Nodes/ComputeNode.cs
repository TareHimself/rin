namespace rsl.Nodes;

/// <summary>
///     compute( <see cref="Tags" /> )
/// </summary>
//size of a workgroup for compute
public class ComputeNode : Node
{
    public readonly Dictionary<string, string> Tags;

    public ComputeNode(Dictionary<string, string> tags ) : base(
        NodeType.Compute)
    {
        Tags = tags;
    }

    public override IEnumerable<Node> GetChildren() => [];
}