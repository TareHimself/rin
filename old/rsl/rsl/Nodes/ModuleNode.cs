namespace rsl.Nodes;

/// <summary>
///     <see cref="Statements" />
/// </summary>
public class ModuleNode : Node
{
    public string FilePath;
    public Node[] Statements;

    public ModuleNode(string filePath, IEnumerable<Node> statements) : base(Nodes.NodeType.Module)
    {
        FilePath = filePath;
        Statements = statements.ToArray();
    }

    public override IEnumerable<Node> GetChildren()
    {
        return Statements;
    }
}