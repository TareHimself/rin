namespace Rin.Shading.Ast.Nodes;

public class UniformNode : INode
{
    public Dictionary<string, string> Tags = [];
    
    public required uint Set { get; set; }
    public required uint Binding { get; set; }
    public required VariableDeclarationNode Declaration { get; set; }
    public IEnumerable<INode> Children => [Declaration];
}