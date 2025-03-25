namespace Rin.Shading.Ast.Nodes;

public class FunctionNode : INode
{
    public required string Name { get; set; }
    public ParameterDeclarationNode[] Params { get; set; } = [];
    public required ScopeNode Scope { get; set; }
    
    public required IType ReturnType { get; set; }
    
    public IEnumerable<INode> Children {
        get
        {
            foreach (var param in Params)
            {
                yield return param;
            }
            
            yield return ReturnType;
            
            yield return Scope;
        }
    }
}