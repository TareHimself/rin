

namespace rsl.Nodes;

public static class Extensions
{
    public static void Walk(this Node start, Func<Node, bool> visitor,bool reversed = false)
    {
        Stack<Node> pending = [];
        pending.Push(start);

        while (pending.Count != 0)
        {
            var node = pending.Pop();
            if (!visitor(node)) return;
            if (reversed)
            {
                foreach (var item in node.GetChildren()) pending.Push(item);
            }
            else
            {
                foreach (var item in node.GetChildren().Reverse()) pending.Push(item);
            }
        }
    }
    
    public static void Walk(this Node start, Func<Node, bool> visitor,Func<Node,IEnumerable<Node>> getChildren)
    {
        Stack<Node> pending = [];
        
        pending.Push(start);
        
        foreach (var item in getChildren(start)) pending.Push(item);

        while (pending.Count != 0)
        {
            var node = pending.Pop();
            if (!visitor(node)) return;
            foreach (var item in getChildren(node)) pending.Push(item);
        }
    }
    
    

    public static void ResolveStructReferences(this Node start)
    {
        Dictionary<string, StructNode> structs = new();
        start.Walk(node =>
        {
            if (node is StructNode stru)
                if (stru.Name != "" && !structs.ContainsKey(stru.Name))
                    structs.Add(stru.Name, stru);

            if (node is StructDeclarationNode struDecl)
                if (struDecl.StructName != "" && structs.TryGetValue(struDecl.StructName, out var structNode))
                    struDecl.Struct = structNode;
            return true;
        });
    }

    public struct NodeSymbols
    {
        public HashSet<string> Calls = [];
        public HashSet<string> Identifiers = [];
        public HashSet<string> Types = [];
        public NodeSymbols()
        {
        }
    }


    public static void CheckSymbols(this Node node, NodeSymbols result)
    {
        node.Walk((walkedNode) =>
        {
                
            if (walkedNode is DeclarationNode asDeclaration)
            {
                if (asDeclaration is not BlockDeclarationNode)
                {
                    result.Types.Add(asDeclaration.GetTypeName());

                    result.Identifiers.Remove(asDeclaration.DeclarationName);
                }
            }
            else if (walkedNode is CallNode asCall)
            {
                result.Calls.Add(asCall.Identifier.Identity);
            }
            else if (walkedNode is AccessNode asAccess)
            {
                Node pos = asAccess;
                while (pos is AccessNode accessNode)
                {
                    pos = accessNode.Left;
                }

                if (pos is IdentifierNode asIdentifier)
                {
                    result.Identifiers.Add(asIdentifier.Identity);
                }
            }
            else if (walkedNode is IdentifierNode asIdentifier)
            {
                result.Identifiers.Add(asIdentifier.Identity);
            }
            return true;
        }, (targetNode) =>
        {
            if (targetNode is AccessNode)
            {
                return [];
            }

            return targetNode.GetChildren().Reverse();
        });
    }

    public static void GetRequiredFunctionSymbols(this FunctionNode node,NodeSymbols result)
    {
        result.Types.Add(node.ReturnDeclaration.GetTypeName());
        
        foreach (var functionArgumentNode in node.Arguments)
        {
            result.Types.Add(functionArgumentNode.Declaration.GetTypeName());
        }
        
        for (var i = node.Scope.Statements.Length - 1; i > -1; i--)
        {
            var statement = node.Scope.Statements[i];
            statement.CheckSymbols(result);
        }
    }
    
    public static ModuleNode? ExtractFunctionWithDependencies(this ModuleNode node,string functionName)
    {
        var functionIdx = -1;
        for (var i = node.Statements.Length - 1; i > -1; i--)
        {
            
            if (node.Statements[i] is FunctionNode asFunctionNode)
            {
                if (asFunctionNode.Name == functionName)
                {
                    functionIdx = i;
                    break;
                }
            }
        }


        if (functionIdx == -1) return null;


        var allSymbols = new NodeSymbols();
        
        ((FunctionNode)node.Statements[functionIdx]).GetRequiredFunctionSymbols(allSymbols);


        List<Node> newStatements = [node.Statements[functionIdx]];
        
        for (var i = functionIdx - 1; i > -1; i--)
        {
            if (node.Statements[i] is FunctionNode asFunctionNode)
            {
                if (allSymbols.Calls.Contains(asFunctionNode.Name))
                {
                    asFunctionNode.GetRequiredFunctionSymbols(allSymbols);
                    newStatements.Add(asFunctionNode);
                }
            }
            else if (node.Statements[i] is DefineNode asDefine)
            {
                if (allSymbols.Identifiers.Contains(asDefine.Identifier))
                {
                    newStatements.Add(asDefine);
                }
            }
            else if (node.Statements[i] is ConstNode asConstNode)
            {
                if (allSymbols.Identifiers.Contains(asConstNode.Declaration.DeclarationName))
                {
                    newStatements.Add(asConstNode);
                    asConstNode.CheckSymbols(allSymbols);
                }
            }
            else if (node.Statements[i] is StructNode asStructNode)
            {
                if (allSymbols.Types.Contains(asStructNode.Name))
                {
                    newStatements.Add(asStructNode);
                    asStructNode.CheckSymbols(allSymbols);
                }
            }
            else if (node.Statements[i] is LayoutNode or PushConstantNode or IncludeNode)
            {
                if(node.Statements[i] is LayoutNode or PushConstantNode) node.Statements[i].CheckSymbols(allSymbols);
                newStatements.Add(node.Statements[i]);
            }
        }

        newStatements.Reverse();
        
        return new ModuleNode(node.FilePath, newStatements);
    }
    
    
    public static NamedScopeNode ResolveIncludes(this ModuleNode node,NamedScopeNode scopeNode, Func<IncludeNode, ModuleNode, string> resolver)
    {
        var newStatements = scopeNode.Statements.ToList();
        var includedFiles = new HashSet<string>();
        for (var i = 0; i < newStatements.Count; i++)
        {
            if (newStatements[i] is not IncludeNode includeNode) continue;

            var absPath = resolver(includeNode, node);

            if (!includedFiles.Add(absPath))
            {
                newStatements.RemoveAt(i);
                i--;
                continue;
            }

            var tokens = Tokenizer.Run(absPath);
            var ast = Parser.Parse(ref tokens);
            newStatements.RemoveAt(i);
            newStatements.InsertRange(i, ast.Statements);
            i--;
        }
        return new NamedScopeNode(scopeNode.ScopeType, newStatements);
    }
    
    public static ModuleNode ResolveIncludes(this ModuleNode node, Func<IncludeNode, ModuleNode, string> resolver)
    {
        var newStatements = node.Statements.ToList();
        var includedFiles = new HashSet<string>();
        var targetNode = node;
        for (var i = 0; i < newStatements.Count; i++)
        {
            if (newStatements[i] is NamedScopeNode namedScope)
            {
                newStatements[i] = ResolveIncludes(node,namedScope,resolver);
            }
            
            if (newStatements[i] is not IncludeNode includeNode) continue;

            var absPath = resolver(includeNode, targetNode);

            if (!includedFiles.Add(absPath))
            {
                newStatements.RemoveAt(i);
                i--;
                continue;
            }

            var tokens = Tokenizer.Run(absPath);
            var ast = Parser.Parse(ref tokens);
            newStatements.RemoveAt(i);
            newStatements.InsertRange(i, ast.Statements);
            i--;
        }
        
        return new ModuleNode(targetNode.FilePath, newStatements);
    }

    public static ModuleNode ExtractScope(this ModuleNode node, ScopeType scopeType)
    {
        var statements = node.Statements.ToList();

        for (var i = 0; i < statements.Count; i++)
        {
            if (statements[i] is NamedScopeNode asNamedScope)
            {
                statements.RemoveAt(i);
                
                if (asNamedScope.ScopeType == scopeType)
                {
                    statements.InsertRange(i, asNamedScope.Statements);
                }
                
                i--;
            }
        }

        return new ModuleNode(node.FilePath, statements);
    }
}