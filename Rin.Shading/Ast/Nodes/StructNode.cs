namespace Rin.Shading.Ast.Nodes;

public class StructNode : INode
{
    public required string Name { get; set; }
    public StructVariableDeclarationNode[] Declarations { get; set; } = [];
    public FunctionNode[] Functions { get; set; } = [];

    public IEnumerable<INode> Children
    {
        get
        {
            foreach (var declaration in Declarations)
            {
                yield return declaration;
            }

            foreach (var function in Functions)
            {
                yield return function;
            }
        }

    }

    public ulong GetSize()
    {
        return Declarations.Aggregate((ulong)0,(t,c) => t + c.GetSize());
    }
}