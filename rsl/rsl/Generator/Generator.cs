using rsl.Nodes;


namespace rsl.Generator;

public abstract class Generator
{
    public string Run(ModuleNode moduleNode, ScopeType targetScope) =>
        Run(moduleNode.ExtractScope(targetScope).Statements.ToList());
    
    public abstract string Run(IEnumerable<Node> nodes);

}