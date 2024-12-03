namespace rsl.Nodes;

/// <summary>
///     #include <see cref="File" />
/// </summary>
public class IncludeNode : Node
{
    public string File;
    public string SourceFile;

    public IncludeNode(string sourceFile, string file) : base(Nodes.NodeType.Include)
    {
        SourceFile = sourceFile;
        File = file;
    }

    public override IEnumerable<Node> GetChildren() => [];
}