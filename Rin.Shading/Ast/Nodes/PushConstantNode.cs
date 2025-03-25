namespace Rin.Shading.Ast.Nodes;

public class PushConstantNode : INode
{
    public IEnumerable<INode> Children => Declarations;
    public VariableDeclarationNode[] Declarations { get; set; } = [];
}