using Rin.Shading.Ast.Nodes;

namespace Rin.Shading;

public class Module
{
    
    public string FullSourcePath { get; set; }
    public INode[] Statements { get; }
    
    public Dictionary<string,INode[]> ScopedStatements { get; }
    
    public Module(string fullSourcePath, INode[] statements)
    {
        FullSourcePath = fullSourcePath;
        Statements = statements;
        Dictionary<string, List<INode>> scopedStatements = [];
        foreach (var statement in Statements)
        {
            if (statement is NamedScopeNode asNamed && !scopedStatements.ContainsKey(asNamed.Name))
            {
                scopedStatements.Add(asNamed.Name, []);
            }
        }
        foreach (var statement in Statements)
        {
            if (statement is NamedScopeNode asNamed)
            {
                scopedStatements[asNamed.Name].AddRange(asNamed.Statements);
            }
            else
            {
                foreach (var statementsList in scopedStatements.Values)
                {
                    statementsList.Add(statement);
                }
            }
        }
        ScopedStatements = scopedStatements.ToDictionary((kv) => kv.Key, (kv) =>
        {
            
            Dictionary<string, StructNode> structs = [];
            kv.Value.Transverse((node) =>
            {
                if (node is StructNode asStructNode)
                {
                    structs.Add(asStructNode.Name, asStructNode);
                }

                if (node is DeclarationNode asDeclarationNode)
                {
                    if (asDeclarationNode.Type is UnknownType asUnknownType)
                    {
                        if (structs.TryGetValue(asUnknownType.TypeName, out var structFound))
                        {
                            asDeclarationNode.Type = new StructTypeNode
                            {
                                Struct = structFound
                            };
                        }
                    }
                }

                return true;
            });
            return kv.Value.ToArray();
        });
    }
    
    public INode[]? TryGetScope(string name)
    {
        return ScopedStatements.GetValueOrDefault(name);
    }
    
    public INode[] GetScope(string name)
    {
        return ScopedStatements[name];
    }
    
    public string[] GetScopeNames()
    {
        return ScopedStatements.Keys.ToArray();
    }
}