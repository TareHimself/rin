namespace rsl.Nodes;

/// <summary>
///     <see cref="IsInput">in/out</see> <see cref="Declaration" />
/// </summary>
public class FunctionArgumentNode : Node
{
    public DeclarationNode Declaration;
    public bool IsInput;

    public FunctionArgumentNode(bool isInput, DeclarationNode declaration) : base(Nodes.NodeType.FunctionArgument)
    {
        IsInput = isInput;
        Declaration = declaration;
    }

    public override IEnumerable<Node> GetChildren()
    {
        return [Declaration];
    }
}