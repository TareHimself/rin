namespace rsl.Nodes;

/// <summary>
///     layout( <see cref="Tags" /> ) <see cref="Qualifier" /> <see cref="Declaration" />
/// </summary>
public class LayoutNode : Node
{
    public readonly DeclarationNode Declaration;
    public readonly LayoutQualifier Qualifier;
    public readonly Dictionary<string, string> Tags;

    public LayoutNode(LayoutQualifier qualifier, DeclarationNode declaration,Dictionary<string, string> tags ) : base(
        NodeType.Layout)
    {
        Tags = tags;
        Qualifier = qualifier;
        Declaration = declaration;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Declaration];
    }
}