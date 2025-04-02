namespace Rin.Shading.Ast.Nodes;

public class PushConstantNode : INode
{
    public VariableDeclarationNode[] Declarations { get; set; } = [];
    public IEnumerable<INode> Children => Declarations;
}